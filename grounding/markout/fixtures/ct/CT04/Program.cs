// A dependency scanner produced this result. Plain data — it carries no output formatting.

var report = new DependencyReport
{
    Project = "Web.Api",
    Dependencies = new()
    {
        new() { Name = "Serilog", Version = "3.1.1" },
        new() { Name = "Polly", Version = "8.2.0" },
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class DependencyReport
{
    public string Project { get; init; } = "";
    public List<Dependency> Dependencies { get; init; } = new();
}

public sealed class Dependency
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
}
