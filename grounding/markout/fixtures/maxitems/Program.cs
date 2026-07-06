// A ranked package list. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Top Packages";

var packages = new List<PkgRow>
{
    new("alpha", "1"),
    new("beta", "2"),
    new("gamma", "3"),
    new("delta", "4"),
};

// TODO: print a GitHub-flavored Markdown report: an H1 title, then a "## Packages" table
// showing only the FIRST 2 packages (truncate the rest) using the library's max-items
// attribute. See task prompt.

public record PkgRow(string Name, string Rank);
