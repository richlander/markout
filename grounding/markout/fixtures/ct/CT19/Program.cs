// A package search service produced these hits. Plain data — it carries no output formatting.

var report = new SearchReport
{
    Query = "logging",
    Results = new()
    {
        new() { Package = "Serilog", Downloads = 500_000_000 },
        new() { Package = "NLog", Downloads = 300_000_000 },
        new() { Package = "log4net", Downloads = 250_000_000 },
        new() { Package = "Microsoft.Extensions.Logging", Downloads = 900_000_000 },
        new() { Package = "Serilog.Sinks.Console", Downloads = 200_000_000 },
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class SearchReport
{
    public string Query { get; init; } = "";
    public List<Hit> Results { get; init; } = new();
}

public sealed class Hit
{
    public string Package { get; init; } = "";
    public long Downloads { get; init; }
}
