// A quality gate produced this result. Plain data — it carries no output formatting.

using Markout;

var report = new QualityGate
{
    Build = "nightly-4821",
    Metrics = new()
    {
        new MetricChange<int>("Failures", 7, 0) { Goal = Goal.Lower },
        new MetricChange<int>("Coverage", 78, 85) { Goal = Goal.Higher },
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class QualityGate
{
    public string Build { get; init; } = "";
    public List<MetricChange<int>> Metrics { get; init; } = new();
}
