// A package collection tool produced this report. Plain data — it carries no output formatting.

var pkg = new PackageReport
{
    Id = "Serilog",
    Downloads = 500_000_000,
    Dependencies = new() { new() { Name = "Serilog.Sinks.Console", Version = "5.0.0" } },
    Diagnostics = new() { new() { Level = "warning", Text = "transitive version conflict" } },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PackageReport
{
    public string Id { get; init; } = "";
    public long Downloads { get; init; }
    public List<Dep> Dependencies { get; init; } = new();
    public List<Diagnostic> Diagnostics { get; init; } = new();
}

public sealed class Dep { public string Name { get; init; } = ""; public string Version { get; init; } = ""; }

public sealed class Diagnostic { public string Level { get; init; } = ""; public string Text { get; init; } = ""; }
