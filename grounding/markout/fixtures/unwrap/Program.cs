// A report with tags and rows. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Report";
var tags = new List<string> { "alpha", "beta" };

var rows = new List<DataRow>
{
    new("x", "1"),
};

// TODO: print a GitHub-flavored Markdown report: an H1 title; render the tags INLINE with no
// "## Tags" heading (use the library's unwrap attribute); render rows under a normal "## Rows"
// section. See task prompt.

public record DataRow(string A, string B);
