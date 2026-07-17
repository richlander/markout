// A test runner produced this result. Plain data — it carries no output formatting.

var run = new TestRun
{
    Suite = "Integration",
    TotalTests = 214,
    Passed = 210,
    InternalRunId = "a91f-cc02",
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class TestRun
{
    public string Suite { get; init; } = "";
    public int TotalTests { get; init; }
    public int Passed { get; init; }
    public string InternalRunId { get; init; } = "";
}
