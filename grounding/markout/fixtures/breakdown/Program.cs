// A scan produced this severity distribution. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Scan Summary";

// Counts by severity (total 20).
var severities = new List<SeverityCount>
{
    new("Critical", 3),
    new("High", 12),
    new("Low", 5),
};

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title, then a
// "## Severity" section rendering the counts as a proportional breakdown (stacked bar)
// using the library's built-in breakdown/segment shapes (do not hand-write the table).

public record SeverityCount(string Category, int Count);
