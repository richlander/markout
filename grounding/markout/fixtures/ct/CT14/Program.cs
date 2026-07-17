// A benchmark runner produced this result. Plain data — it carries no output formatting.

var report = new BenchReport
{
    Suite = "Serialization",
    Results = new()
    {
        new() { Name = "Utf8", OpsPerSec = 1_250_000, AllocatedKb = 1.4 },
        new() { Name = "Newtonsoft", OpsPerSec = 410_000, AllocatedKb = 8.2 },
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class BenchReport
{
    public string Suite { get; init; } = "";
    public List<BenchRow> Results { get; init; } = new();
}

public sealed class BenchRow
{
    public string Name { get; init; } = "";
    public long OpsPerSec { get; init; }
    public double AllocatedKb { get; init; }
}
