// A dependency resolver produced these package graphs. Plain data — it carries no output formatting.

var packages = new List<PackageDeps>
{
    new() { Id = "Serilog", Dependencies = new()
        {
            new() { Name = "Serilog.Sinks.Console", Version = "5.0.0", Optional = false },
            new() { Name = "Serilog.Enrichers.Thread", Version = "3.1.0", Optional = false },
        } },
    new() { Id = "MyApp", Dependencies = new()
        {
            new() { Name = "Serilog", Version = "3.1.1", Optional = false },
            new() { Name = "BenchmarkDotNet", Version = "0.13.12", Optional = true },
        } },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PackageDeps
{
    public string Id { get; init; } = "";
    public List<Dep> Dependencies { get; init; } = new();
}

public sealed class Dep
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public bool Optional { get; init; }
}
