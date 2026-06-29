// A GitHub API query produced these issues. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Open Issues";

var issues = new List<IssueData>
{
    new("Crash on startup", "https://github.com/acme/app/issues/1", "open", "v1.0"),
    new("Typo in docs", "https://github.com/acme/app/issues/2", "closed", "v1.0"),
    new("Add dark mode", "https://github.com/acme/app/issues/3", "open", "v2.0"),
};

// TODO: print a Markdown report of these issues (see task prompt).

public record IssueData(string Title, string Url, string State, string Milestone);
