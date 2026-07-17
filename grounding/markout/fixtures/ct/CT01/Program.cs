// A build tool produced this result. Plain data — it carries no output formatting.

var report = new BuildReport
{
    Project = "Web.Api",
    Configuration = "Release",
    Warnings = 3,
    Errors = 0,
};

// TODO: Using the referenced Markout library, print a Markdown report whose H1 title is the
// project name, followed by a Field | Value table of the remaining scalar values. Produce the
// output through Markout's serializer (do not hand-write the Markdown). Build and run to confirm.

public sealed class BuildReport
{
    public string Project { get; init; } = "";
    public string Configuration { get; init; } = "";
    public int Warnings { get; init; }
    public int Errors { get; init; }
}
