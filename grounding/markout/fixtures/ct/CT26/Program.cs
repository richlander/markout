// A package inspection tool produced this data. Gathering EACH detail section is expensive:
// ScanDependencies() and ScanDiagnostics() stand in for deep scans and each announces itself on
// stderr when it runs. Plain data — it carries no output formatting.

string request = args.Length > 0 ? args[0] : "quiet";   // "quiet" | "Diagnostics" | "all"

var report = new PackageReport { Id = "Serilog", Downloads = 500_000_000 };
// Dependencies and Diagnostics are gathered on demand via the scans below. Gather ONLY the
// section(s) the request actually renders.

// TODO: Using the referenced Markout library, render `report` for the requested selector from ONE
// model + one render path:
//   - "quiet":       the title + scalar fields only; gather NEITHER section.
//   - "Diagnostics": render ONLY "## Diagnostics" — promote collection to gather it (ScanDiagnostics),
//                    but do NOT gather Dependencies.
//   - "all":         render BOTH "## Dependencies" and "## Diagnostics" — gather both.
// Select rendered sections with IncludeSections; gate what you COLLECT on the request so a narrow
// request stays cheap. Do not hand-write omitted sections. Build and run to confirm.

static List<Dep> ScanDependencies()
{
    Console.Error.WriteLine("SCAN:dependencies");   // the expensive dependencies scan ran
    return new() { new() { Name = "Serilog.Sinks.Console", Version = "5.0.0" } };
}

static List<Diagnostic> ScanDiagnostics()
{
    Console.Error.WriteLine("SCAN:diagnostics");   // the expensive diagnostics scan ran
    return new() { new() { Level = "warning", Text = "transitive version conflict" } };
}

public sealed class PackageReport
{
    public string Id { get; init; } = "";
    public long Downloads { get; init; }
    public List<Dep> Dependencies { get; set; } = new();
    public List<Diagnostic> Diagnostics { get; set; } = new();
}

public sealed class Dep { public string Name { get; init; } = ""; public string Version { get; init; } = ""; }
public sealed class Diagnostic { public string Level { get; init; } = ""; public string Text { get; init; } = ""; }
