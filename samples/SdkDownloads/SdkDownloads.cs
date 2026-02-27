// Streaming zero-allocation .NET SDK download links
// Demonstrates: HTTP stream → Utf8JsonReader (zero-alloc) → MarkoutWriter streaming table → stdout
//
// Architecture:
//   1. Small index file parsed with JsonDocument (pooled, ~4KB)
//   2. Large per-channel releases.json streamed with Utf8JsonReader (zero-alloc ref struct)
//   3. Markdown rows written directly from the parse loop — no intermediate collections
//   4. Buffer rented from ArrayPool — no GC pressure
//
// The only main-loop allocations are Utf8JsonReader.GetString() calls (3 per row),
// required by the current WriteRow(ReadOnlySpan<string>) API surface.
// A future Span<byte>-based WriteRow overload would eliminate those too.

using System.Buffers;
using System.Text.Json;
using Markout;

const string IndexUrl = "https://github.com/dotnet/core/raw/refs/heads/main/release-notes/releases-index.json";

using var http = new HttpClient();
var formatter = new MarkdownFormatter();
var options = new MarkoutWriterOptions { BoldFieldNames = true };
var writer = new MarkoutWriter(Console.Out, formatter, options);

// ── Phase 1: Fetch the small index file (JsonDocument is fine here — ~4KB) ──

using var indexStream = await http.GetStreamAsync(IndexUrl);
using var indexDoc = await JsonDocument.ParseAsync(indexStream);

writer.WriteHeading(1, ".NET SDK Downloads");

// Row buffer — allocated once, reused for every row across all channels
var row = new string[2];

// Rent a buffer from the pool — zero GC pressure for the streaming reader
var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
try
{
    var channels = indexDoc.RootElement.GetProperty("releases-index");

    foreach (var channel in channels.EnumerateArray())
    {
        var version = channel.GetProperty("channel-version").GetString()!;
        var supportPhase = channel.GetProperty("support-phase").GetString()!;
        var releasesUrl = channel.GetProperty("releases.json").GetString()!;

        var releaseType = channel.TryGetProperty("release-type", out var rt)
            ? rt.GetString()?.ToUpperInvariant()
            : null;

        // Section heading with context
        var context = releaseType != null ? $"{releaseType}, {supportPhase}" : supportPhase;
        writer.WriteHeading(2, $".NET {version}", context);

        // ── Phase 2: Stream the large releases.json with Utf8JsonReader ──
        // Only parse releases[0].sdk — early exit skips the rest of the file.

        using var releaseStream = await http.GetStreamAsync(releasesUrl);

        var fileCount = await StreamSdkFiles(releaseStream, buffer, writer, row);

        if (fileCount == 0)
            writer.WriteParagraph("*No SDK files available.*");
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// ── Streaming JSON reader + Markdown writer ──
// Reads network stream in chunks, parses with Utf8JsonReader (zero-alloc ref struct),
// and writes markdown table rows directly from the parse loop.
// The Utf8JsonReader never crosses an await boundary.

static async Task<int> StreamSdkFiles(Stream stream, byte[] buffer, MarkoutWriter writer, string[] row)
{
    int dataLength = 0;
    var readerState = new JsonReaderState();
    var nav = new JsonNavigator();
    bool tableStarted = false;
    int fileCount = 0;

    while (true)
    {
        // Read next chunk from network into the buffer
        int bytesRead = await stream.ReadAsync(buffer.AsMemory(dataLength));
        dataLength += bytesRead;
        bool isFinalBlock = bytesRead == 0;

        // Parse the buffered data — Utf8JsonReader is a ref struct, zero-alloc
        var reader = new Utf8JsonReader(buffer.AsSpan(0, dataLength), isFinalBlock, readerState);

        while (reader.Read())
        {
            nav.ProcessToken(ref reader);

            // When the navigator completes a file object, write it immediately
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

                // Reuse the row buffer — no allocation here
                row[0] = nav.FileRid!;
                row[1] = string.Concat("[", nav.FileName!, "](", nav.FileUrl!, ")");
                writer.WriteTableRow(row);
                fileCount++;
                nav.ConsumeFile();
            }

            if (nav.Done)
                break;
        }

        if (nav.Done || isFinalBlock)
            break;

        // Save reader state and compact the buffer
        readerState = reader.CurrentState;
        int consumed = (int)reader.BytesConsumed;
        int remaining = dataLength - consumed;
        if (remaining > 0)
            Buffer.BlockCopy(buffer, consumed, buffer, 0, remaining);
        dataLength = remaining;
    }

    if (tableStarted)
        writer.WriteTableEnd();

    return fileCount;
}

/// <summary>
/// State machine that navigates the releases.json structure using Utf8JsonReader tokens.
/// Extracts: releases[0].release-date, releases[0].sdk.version, releases[0].sdk.files[*].{name,rid,url}
///
/// Call ProcessToken() for each reader.Read(), then check HasFile/Done.
/// When HasFile is true, read FileRid/FileName/FileUrl, then call ConsumeFile().
/// When Done is true, stop reading — we've consumed everything we need from releases[0].
/// </summary>
struct JsonNavigator
{
    private int _depth;
    private Phase _phase;
    private string? _currentProperty;

    // Extracted metadata (set once)
    public string? SdkVersion { get; private set; }
    public string? ReleaseDate { get; private set; }

    // Current file (set per file object, cleared by ConsumeFile)
    public string? FileRid { get; private set; }
    public string? FileName { get; private set; }
    public string? FileUrl { get; private set; }
    public bool HasFile { get; private set; }
    public bool Done { get; private set; }

    public void ConsumeFile()
    {
        HasFile = false;
        FileRid = null;
        FileName = null;
        FileUrl = null;
    }

    enum Phase
    {
        SeekingReleases,
        InReleasesArray,
        InFirstRelease,
        InSdk,
        InFilesArray,
        InFileObject,
    }

    public void ProcessToken(ref Utf8JsonReader reader)
    {
        var tokenType = reader.TokenType;

        switch (_phase)
        {
            case Phase.SeekingReleases:
                if (tokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("releases"u8))
                    _phase = Phase.InReleasesArray;
                break;

            case Phase.InReleasesArray:
                if (tokenType == JsonTokenType.StartObject)
                {
                    _phase = Phase.InFirstRelease;
                    _depth = 0;
                }
                break;

            case Phase.InFirstRelease:
                if (tokenType == JsonTokenType.PropertyName && _depth == 0)
                {
                    if (reader.ValueTextEquals("sdk"u8))
                        _phase = Phase.InSdk;
                    else if (reader.ValueTextEquals("release-date"u8))
                        _currentProperty = "release-date";
                    else
                        _currentProperty = null;
                }
                else if (tokenType == JsonTokenType.String && _currentProperty == "release-date")
                {
                    ReleaseDate = reader.GetString();
                    _currentProperty = null;
                }
                else if (tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    _depth++;
                else if (tokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                {
                    if (--_depth < 0) Done = true; // exited releases[0]
                }
                break;

            case Phase.InSdk:
                if (tokenType == JsonTokenType.StartObject)
                    _depth = 0;
                else if (tokenType == JsonTokenType.PropertyName && _depth == 0)
                {
                    if (reader.ValueTextEquals("version"u8))
                        _currentProperty = "version";
                    else if (reader.ValueTextEquals("files"u8))
                        _phase = Phase.InFilesArray;
                    else
                        _currentProperty = null;
                }
                else if (tokenType == JsonTokenType.String && _currentProperty == "version")
                {
                    SdkVersion = reader.GetString();
                    _currentProperty = null;
                }
                else if (tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    _depth++;
                else if (tokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                {
                    if (--_depth < 0) Done = true; // exited sdk object
                }
                break;

            case Phase.InFilesArray:
                if (tokenType == JsonTokenType.StartObject)
                {
                    _phase = Phase.InFileObject;
                    FileRid = null;
                    FileName = null;
                    FileUrl = null;
                    _depth = 0;
                }
                else if (tokenType == JsonTokenType.EndArray)
                    Done = true;
                break;

            case Phase.InFileObject:
                if (tokenType == JsonTokenType.PropertyName && _depth == 0)
                {
                    if (reader.ValueTextEquals("rid"u8))
                        _currentProperty = "rid";
                    else if (reader.ValueTextEquals("name"u8))
                        _currentProperty = "name";
                    else if (reader.ValueTextEquals("url"u8))
                        _currentProperty = "url";
                    else
                        _currentProperty = null;
                }
                else if (tokenType == JsonTokenType.String && _currentProperty != null)
                {
                    // GetString() is the one unavoidable allocation per field —
                    // it materializes the UTF-8 span as a managed string.
                    // Eliminating this would require a Span<byte>-based WriteRow overload.
                    switch (_currentProperty)
                    {
                        case "rid": FileRid = reader.GetString(); break;
                        case "name": FileName = reader.GetString(); break;
                        case "url": FileUrl = reader.GetString(); break;
                    }
                    _currentProperty = null;
                }
                else if (tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                    _depth++;
                else if (tokenType == JsonTokenType.EndObject)
                {
                    if (_depth == 0)
                    {
                        // File object complete — signal the caller
                        if (FileRid != null && FileName != null && FileUrl != null)
                            HasFile = true;
                        _phase = Phase.InFilesArray;
                    }
                    else _depth--;
                }
                else if (tokenType == JsonTokenType.EndArray)
                    _depth--;
                break;
        }
    }
}
