// A release tool produced this result. Plain data — it carries no output formatting.

var report = new ReleaseReport
{
    Release = "v2.4.0",
    Commits = 87,
    Contributors = 12,
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class ReleaseReport
{
    public string Release { get; init; } = "";
    public int Commits { get; init; }
    public int Contributors { get; init; }
}
