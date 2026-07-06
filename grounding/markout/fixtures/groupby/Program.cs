// A list of issues. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Open Issues";

var issues = new List<IssueRow>
{
    new("Crash on startup", "v1.0"),
    new("Slow query", "v2.0"),
    new("Typo in docs", "v1.0"),
};

// TODO: print a GitHub-flavored Markdown report: an H1 title, then an "## Issues" section that
// GROUPS the issues by milestone — one "### <milestone>" subheading per group — using the
// section's group-by option. See task prompt.

public record IssueRow(string Title, string Milestone);
