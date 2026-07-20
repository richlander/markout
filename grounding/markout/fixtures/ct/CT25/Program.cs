// A package inspection tool produced this data. Gathering Diagnostics is the EXPENSIVE step:
// ScanDiagnostics() stands in for a deep scan and announces itself on stderr when it runs.
// Plain data — it carries no output formatting.

string mode = args.Length > 0 ? args[0] : "quiet";   // "quiet" | "detail"

var report = new PackageReport
{
    Id = "Serilog",
    Downloads = 500_000_000,
    // Diagnostics is deliberately NOT populated here — it must be gathered on demand via
    // ScanDiagnostics(), and only when the requested detail level actually renders it.
};

// TODO: Using the referenced Markout library, render `report` for the requested mode from ONE
// model + one render path:
//   - "quiet":  the title + scalar fields only. Do NOT gather Diagnostics (do not call ScanDiagnostics).
//   - "detail": additionally render a "## Diagnostics" section — gather it by calling ScanDiagnostics().
// Choose what renders with IncludeSections; gate what you COLLECT on the mode so quiet stays cheap.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

static List<Diagnostic> ScanDiagnostics()
{
    Console.Error.WriteLine("SCAN:diagnostics");   // the expensive scan ran (observable side effect)
    return new() { new() { Level = "warning", Text = "transitive version conflict" } };
}

public sealed class PackageReport
{
    public string Id { get; init; } = "";
    public long Downloads { get; init; }
    public List<Diagnostic> Diagnostics { get; set; } = new();
}

public sealed class Diagnostic { public string Level { get; init; } = ""; public string Text { get; init; } = ""; }
