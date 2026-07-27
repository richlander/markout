// An experiment tracker produced this comparison. Plain data — it carries no output formatting.

using Markout;

var report = new ModelComparison
{
    Experiment = "grounding-eval",
    Rows = new()
    {
        new MultiSourceRow("Tasks solved",
            new Source("baseline", new Fraction(4, 6)),
            new Source("grounded", new Fraction(6, 6))),
        new MultiSourceRow("Cost share",
            new Source("baseline", new Share(5056, 21000)),
            new Source("grounded", new Share(2000, 21000))),
        new MultiSourceRow("Pass rate",
            new Source("baseline", new Percent(67, 100)),
            new Source("grounded", new Percent(100, 100))),
        new MultiSourceRow("Tool mix",
            new Source("baseline", new Segments(new Segment("web", 21), new Segment("cache", 8))),
            new Source("grounded", new Segments(new Segment("web", 3), new Segment("cache", 26)))),
        new MultiSourceRow("Verdict",
            new Source("baseline", new Verdict(GateStatus.Warning, "hedged")),
            new Source("grounded", new Verdict(GateStatus.Good, "clean"))),
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class ModelComparison
{
    public string Experiment { get; init; } = "";
    public List<MultiSourceRow> Rows { get; init; } = new();
}
