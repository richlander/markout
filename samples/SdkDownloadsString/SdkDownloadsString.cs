// Standard string-based .NET SDK download links — through Markout
//
// Same output as the byte-streaming version, but uses the standard
// MarkoutWriter string API: WriteTableRow(ReadOnlySpan<string>).
// Every cell value goes through GetString() and string.Concat().
//
// This is the baseline for allocation comparison via dotnet-trace.

using System.Buffers;
using System.Text.Json;
using Markout;

const string IndexUrl = "https://github.com/dotnet/core/raw/refs/heads/main/release-notes/releases-index.json";

using var http = new HttpClient();
var formatter = new MarkdownFormatter();
var options = new MarkoutWriterOptions { BoldFieldNames = true };
var writer = new MarkoutWriter(Console.Out, formatter, options);

// ── Phase 1: Small index file ──

using var indexStream = await http.GetStreamAsync(IndexUrl);
using var indexDoc = await JsonDocument.ParseAsync(indexStream);

writer.WriteHeading(1, ".NET SDK Downloads");

var readBuf = ArrayPool<byte>.Shared.Rent(16 * 1024);
var row = new string[2];

long allocBefore = GC.GetTotalAllocatedBytes(precise: true);
int totalRows = 0;
long renderAlloc = 0;
long[] renderAllocArr = [0];

try
{
    foreach (var channel in indexDoc.RootElement.GetProperty("releases-index").EnumerateArray())
    {
        var version = channel.GetProperty("channel-version").GetString()!;
        var supportPhase = channel.GetProperty("support-phase").GetString()!;
        var releasesUrl = channel.GetProperty("releases.json").GetString()!;
        var releaseType = channel.TryGetProperty("release-type", out var rt)
            ? rt.GetString()?.ToUpperInvariant() : null;

        var context = releaseType != null ? $"{releaseType}, {supportPhase}" : supportPhase;
        writer.WriteHeading(2, $".NET {version}", context);

        using var releaseStream = await http.GetStreamAsync(releasesUrl);
        totalRows += await StreamSdkFiles(releaseStream, readBuf, writer, row, renderAllocArr);
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(readBuf);
}

renderAlloc = renderAllocArr[0];
long allocAfter = GC.GetTotalAllocatedBytes(precise: true);
long totalAlloc = allocAfter - allocBefore;
Console.Error.WriteLine();
Console.Error.WriteLine($"[SdkDownloadsString — standard string API]");
Console.Error.WriteLine($"  Rows:          {totalRows}");
Console.Error.WriteLine($"  Total alloc:   {totalAlloc:N0} bytes ({totalAlloc / 1024.0:N1} KB)");
Console.Error.WriteLine($"  Render alloc:  {renderAlloc:N0} bytes ({renderAlloc / 1024.0:N1} KB)");
Console.Error.WriteLine($"  Per row:       {(totalRows > 0 ? renderAlloc / totalRows : 0):N0} bytes");

/// <summary>
/// Same streaming JSON reader, but uses GetString() + WriteTableRow(ReadOnlySpan&lt;string&gt;).
/// </summary>
static async Task<int> StreamSdkFiles(Stream stream, byte[] readBuf, MarkoutWriter writer, string[] row, long[] renderAlloc)
{
    int dataLen = 0;
    int rowCount = 0;
    var readerState = new JsonReaderState();
    var nav = new StringJsonNavigator();
    bool tableStarted = false;

    while (true)
    {
        int bytesRead = await stream.ReadAsync(readBuf.AsMemory(dataLen));
        dataLen += bytesRead;
        bool isFinalBlock = bytesRead == 0;

        var reader = new Utf8JsonReader(readBuf.AsSpan(0, dataLen), isFinalBlock, readerState);

        while (reader.Read())
        {
            nav.ProcessToken(ref reader);

            if (nav.HasFile)
            {
                if (!tableStarted)
                {
                    writer.WriteFieldsInline(
                        new MarkoutField("SDK", nav.SdkVersion ?? ""),
                        new MarkoutField("Released", nav.ReleaseDate ?? ""));

                    writer.WriteTableStart("Platform", "Download");
                    tableStarted = true;
                }

                long before = GC.GetTotalAllocatedBytes(precise: true);
                row[0] = nav.FileRid!;
                row[1] = string.Concat("[", nav.FileName!, "](", nav.FileUrl!, ")");
                writer.WriteTableRow(row);
                renderAlloc[0] += GC.GetTotalAllocatedBytes(precise: true) - before;
                rowCount++;
                nav.ConsumeFile();
            }

            if (nav.Done) break;
        }

        if (nav.Done || isFinalBlock) break;

        readerState = reader.CurrentState;
        int consumed = (int)reader.BytesConsumed;
        int remaining = dataLen - consumed;
        if (remaining > 0)
            Buffer.BlockCopy(readBuf, consumed, readBuf, 0, remaining);
        dataLen = remaining;
    }

    if (tableStarted)
        writer.WriteTableEnd();
    else
        writer.WriteParagraph("*No SDK files available.*");

    return rowCount;
}

/// <summary>
/// Same navigator but stores strings via GetString() — the standard approach.
/// </summary>
struct StringJsonNavigator
{
    private int _depth;
    private Phase _phase;
    private FieldKind _currentField;

    public string? SdkVersion { get; private set; }
    public string? ReleaseDate { get; private set; }
    public string? FileRid { get; private set; }
    public string? FileName { get; private set; }
    public string? FileUrl { get; private set; }
    public bool HasFile { get; private set; }
    public bool Done { get; private set; }

    public void ConsumeFile() { HasFile = false; FileRid = null; FileName = null; FileUrl = null; }

    enum Phase { SeekingReleases, InReleasesArray, InFirstRelease, InSdk, InFilesArray, InFileObject }
    enum FieldKind { None, ReleaseDate, SdkVersion, Rid, Name, Url }

    public void ProcessToken(ref Utf8JsonReader reader)
    {
        var tt = reader.TokenType;

        switch (_phase)
        {
            case Phase.SeekingReleases:
                if (tt == JsonTokenType.PropertyName && reader.ValueTextEquals("releases"u8))
                    _phase = Phase.InReleasesArray;
                break;

            case Phase.InReleasesArray:
                if (tt == JsonTokenType.StartObject) { _phase = Phase.InFirstRelease; _depth = 0; }
                break;

            case Phase.InFirstRelease:
                if (tt == JsonTokenType.PropertyName && _depth == 0)
                {
                    if (reader.ValueTextEquals("sdk"u8)) _phase = Phase.InSdk;
                    else if (reader.ValueTextEquals("release-date"u8)) _currentField = FieldKind.ReleaseDate;
                    else _currentField = FieldKind.None;
                }
                else if (tt == JsonTokenType.String && _currentField == FieldKind.ReleaseDate)
                { ReleaseDate = reader.GetString(); _currentField = FieldKind.None; }
                else if (tt is JsonTokenType.StartObject or JsonTokenType.StartArray) _depth++;
                else if (tt is JsonTokenType.EndObject or JsonTokenType.EndArray) { if (--_depth < 0) Done = true; }
                break;

            case Phase.InSdk:
                if (tt == JsonTokenType.StartObject) _depth = 0;
                else if (tt == JsonTokenType.PropertyName && _depth == 0)
                {
                    if (reader.ValueTextEquals("version"u8)) _currentField = FieldKind.SdkVersion;
                    else if (reader.ValueTextEquals("files"u8)) _phase = Phase.InFilesArray;
                    else _currentField = FieldKind.None;
                }
                else if (tt == JsonTokenType.String && _currentField == FieldKind.SdkVersion)
                { SdkVersion = reader.GetString(); _currentField = FieldKind.None; }
                else if (tt is JsonTokenType.StartObject or JsonTokenType.StartArray) _depth++;
                else if (tt is JsonTokenType.EndObject or JsonTokenType.EndArray) { if (--_depth < 0) Done = true; }
                break;

            case Phase.InFilesArray:
                if (tt == JsonTokenType.StartObject)
                { _phase = Phase.InFileObject; FileRid = null; FileName = null; FileUrl = null; _depth = 0; }
                else if (tt == JsonTokenType.EndArray) Done = true;
                break;

            case Phase.InFileObject:
                if (tt == JsonTokenType.PropertyName && _depth == 0)
                {
                    if (reader.ValueTextEquals("rid"u8)) _currentField = FieldKind.Rid;
                    else if (reader.ValueTextEquals("name"u8)) _currentField = FieldKind.Name;
                    else if (reader.ValueTextEquals("url"u8)) _currentField = FieldKind.Url;
                    else _currentField = FieldKind.None;
                }
                else if (tt == JsonTokenType.String && _currentField != FieldKind.None)
                {
                    // STRING ALLOCATION: GetString() materializes UTF-8 span as managed string
                    switch (_currentField)
                    {
                        case FieldKind.Rid: FileRid = reader.GetString(); break;
                        case FieldKind.Name: FileName = reader.GetString(); break;
                        case FieldKind.Url: FileUrl = reader.GetString(); break;
                    }
                    _currentField = FieldKind.None;
                }
                else if (tt is JsonTokenType.StartObject or JsonTokenType.StartArray) _depth++;
                else if (tt == JsonTokenType.EndObject)
                {
                    if (_depth == 0)
                    {
                        if (FileRid != null && FileName != null && FileUrl != null) HasFile = true;
                        _phase = Phase.InFilesArray;
                    }
                    else _depth--;
                }
                else if (tt == JsonTokenType.EndArray) _depth--;
                break;
        }
    }
}
