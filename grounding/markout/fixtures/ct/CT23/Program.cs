// A benchmark comparison tool produced this report. Plain data — it carries no output formatting.

using Markout;

var report = new BenchReport
{
    Suite = "Serialization",
    Benchmarks = new()
    {
        new() { Name = "Utf8Writer", Ops = new Change<long>(98555, 61190) },
        new() { Name = "Newtonsoft", Ops = new Change<long>(41000, 39000) },
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class BenchReport
{
    public string Suite { get; init; } = "";
    public List<Bench> Benchmarks { get; init; } = new();
}

public sealed class Bench
{
    public string Name { get; init; } = "";
    public Change<long> Ops { get; init; }
}
