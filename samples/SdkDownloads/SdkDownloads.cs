// Streaming zero-allocation .NET SDK download links — through Markout
//
// Data flow: HTTP stream → Utf8JsonReader.ValueSpan → MarkoutWriter.WriteUtf8() → Stream → stdout
//
// The per-row hot loop has zero allocations:
//   - Utf8JsonReader.ValueSpan → ReadOnlySpan<byte> into the read buffer (no copy, no string)
//   - ValueSpan bytes memcpy'd to staging buffer (no allocation)
//   - MarkoutWriter.BeginTableRow/Cell/WriteUtf8/EndCell/EndRow write directly to Stream
//   - MarkdownFormatter.IUtf8StreamingTableFormatter writes pipe/space bytes to Stream
//
// String-based methods (WriteHeading, WriteFieldsInline) are used for per-section
// headings and metadata — not in the hot loop, so string allocation is acceptable.

using System.Buffers;
using System.Text.Json;
using Markout;

const string IndexUrl = "https://github.com/dotnet/core/raw/refs/heads/main/release-notes/releases-index.json";

using var http = new HttpClient();
var output = Console.OpenStandardOutput();

// MarkoutWriter constructed with Stream — enables both string and byte paths
var formatter = new MarkdownFormatter();
var options = new MarkoutWriterOptions { BoldFieldNames = true };
var writer = new MarkoutWriter(output, formatter, options);

// ── Phase 1: Small index file — JsonDocument is fine (~4KB, once) ──

using var indexStream = await http.GetStreamAsync(IndexUrl);
using var indexDoc = await JsonDocument.ParseAsync(indexStream);

writer.WriteHeading(1, ".NET SDK Downloads");

// Read buffer for streaming JSON (rented once, reused across all channels)
var readBuf = ArrayPool<byte>.Shared.Rent(16 * 1024);

// Staging buffer for per-row cell values (rented once, reused across all rows)
var staging = ArrayPool<byte>.Shared.Rent(1024);

try
{
    foreach (var channel in indexDoc.RootElement.GetProperty("releases-index").EnumerateArray())
    {
        var version = channel.GetProperty("channel-version").GetString()!;
        var supportPhase = channel.GetProperty("support-phase").GetString()!;
        var releasesUrl = channel.GetProperty("releases.json").GetString()!;
        var releaseType = channel.TryGetProperty("release-type", out var rt)
            ? rt.GetString()?.ToUpperInvariant() : null;

        // String-based heading — fine, once per section
        var context = releaseType != null ? $"{releaseType}, {supportPhase}" : supportPhase;
        writer.WriteHeading(2, $".NET {version}", context);

        // ── Phase 2: Stream large releases.json with Utf8JsonReader ──
        using var releaseStream = await http.GetStreamAsync(releasesUrl);
        await StreamSdkFiles(releaseStream, readBuf, staging, writer);
    }

    output.Flush();
}
finally
{
    ArrayPool<byte>.Shared.Return(readBuf);
    ArrayPool<byte>.Shared.Return(staging);
}

/// <summary>
/// Streams releases.json, extracting SDK files from releases[0].sdk.files.
/// The hot loop writes table rows through MarkoutWriter's byte-based API — zero allocation.
/// </summary>
static async Task StreamSdkFiles(Stream stream, byte[] readBuf, byte[] staging, MarkoutWriter writer)
{
    int dataLen = 0;
    var readerState = new JsonReaderState();
    var nav = new JsonNavigator();
    bool tableStarted = false;

    while (true)
    {
        int bytesRead = await stream.ReadAsync(readBuf.AsMemory(dataLen));
        dataLen += bytesRead;
        bool isFinalBlock = bytesRead == 0;

        var reader = new Utf8JsonReader(readBuf.AsSpan(0, dataLen), isFinalBlock, readerState);

        while (reader.Read())
        {
            nav.ProcessToken(ref reader, staging);

            if (nav.HasFile)
            {
                if (!tableStarted)
                {
                    // String-based fields — once per section, not hot path
                    writer.WriteFieldsInline(
                        new MarkoutField("SDK", nav.SdkVersion ?? ""),
                        new MarkoutField("Released", nav.ReleaseDate ?? ""));

                    // String-based headers — once per table
                    writer.WriteTableStart("Platform", "Download");
                    tableStarted = true;
                }

                // ── HOT PATH: zero allocations through Markout ──
                writer.BeginTableRow();

                // Cell 1: platform RID — single span
                writer.WriteTableCellUtf8(staging.AsSpan(nav.RidOffset, nav.RidLength));

                // Cell 2: markdown link — multi-part, no staging assembly needed
                writer.BeginTableCell();
                writer.WriteUtf8("["u8);
                writer.WriteUtf8(staging.AsSpan(nav.NameOffset, nav.NameLength));
                writer.WriteUtf8("]("u8);
                writer.WriteUtf8(staging.AsSpan(nav.UrlOffset, nav.UrlLength));
                writer.WriteUtf8(")"u8);
                writer.EndTableCell();

                writer.EndTableRow();
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
}

/// <summary>
/// State machine navigating releases.json to extract releases[0].sdk.files.
/// Per-row values (rid, name, url) are staged as raw UTF-8 bytes via ValueSpan.
/// Metadata (sdk version, release date) use GetString() — once per channel, not hot path.
/// </summary>
struct JsonNavigator
{
    private int _depth;
    private Phase _phase;
    private FieldKind _currentField;
    private int _stagingPos;

    public string? SdkVersion { get; private set; }
    public string? ReleaseDate { get; private set; }

    public int RidOffset { get; private set; }
    public int RidLength { get; private set; }
    public int NameOffset { get; private set; }
    public int NameLength { get; private set; }
    public int UrlOffset { get; private set; }
    public int UrlLength { get; private set; }

    public bool HasFile { get; private set; }
    public bool Done { get; private set; }

    public void ConsumeFile() { HasFile = false; }

    enum Phase { SeekingReleases, InReleasesArray, InFirstRelease, InSdk, InFilesArray, InFileObject }
    enum FieldKind { None, ReleaseDate, SdkVersion, Rid, Name, Url }

    public void ProcessToken(ref Utf8JsonReader reader, byte[] staging)
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
                { _phase = Phase.InFileObject; _stagingPos = 0; _depth = 0; }
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
                    // CORE ZERO-ALLOC: ValueSpan → staging buffer memcpy. No GetString().
                    var span = reader.ValueSpan;
                    switch (_currentField)
                    {
                        case FieldKind.Rid:
                            RidOffset = _stagingPos;
                            span.CopyTo(staging.AsSpan(_stagingPos));
                            RidLength = span.Length;
                            _stagingPos += span.Length;
                            break;
                        case FieldKind.Name:
                            NameOffset = _stagingPos;
                            span.CopyTo(staging.AsSpan(_stagingPos));
                            NameLength = span.Length;
                            _stagingPos += span.Length;
                            break;
                        case FieldKind.Url:
                            UrlOffset = _stagingPos;
                            span.CopyTo(staging.AsSpan(_stagingPos));
                            UrlLength = span.Length;
                            _stagingPos += span.Length;
                            break;
                    }
                    _currentField = FieldKind.None;
                }
                else if (tt is JsonTokenType.StartObject or JsonTokenType.StartArray) _depth++;
                else if (tt == JsonTokenType.EndObject)
                {
                    if (_depth == 0)
                    {
                        if (RidLength > 0 && NameLength > 0 && UrlLength > 0) HasFile = true;
                        _phase = Phase.InFilesArray;
                    }
                    else _depth--;
                }
                else if (tt == JsonTokenType.EndArray) _depth--;
                break;
        }
    }
}
