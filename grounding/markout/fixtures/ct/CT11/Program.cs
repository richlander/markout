// A regression analyzer produced this comparison. Plain data — it carries no output formatting.

using Markout;

var report = new RegressionReport
{
    Title = "Nightly vs Baseline",
    Errors = new Change<int>(12, 3),
    Coverage = new Change<int>(78, 85),
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class RegressionReport
{
    public string Title { get; init; } = "";
    public Change<int> Errors { get; init; }
    public Change<int> Coverage { get; init; }
}
