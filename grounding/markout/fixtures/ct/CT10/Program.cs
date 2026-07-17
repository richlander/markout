// A performance collector produced this report. Plain data — it carries no output formatting.

using Markout;

var perf = new PerfReport
{
    Title = "Build Performance",
    Timings = new() { new("Restore", 2.1), new("Compile", 4.6), new("Test", 3.2) },
    Coverage = new Breakdown("Coverage", new[] { new Slice("Covered", 82), new Slice("Uncovered", 18) }),
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PerfReport
{
    public string Title { get; init; } = "";
    public List<Metric> Timings { get; init; } = new();
    public Breakdown Coverage { get; init; }
}
