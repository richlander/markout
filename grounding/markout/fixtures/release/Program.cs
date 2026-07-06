// A release produced these features and bug fixes. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var version = "v4.1";

var features = new List<FeatureData>
{
    new("Dark mode", "ana"),
    new("Export CSV", "lee"),
};

var fixes = new List<FixData>
{
    new("#812", "Fix crash on launch"),
};

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title from the
// version, a "## Features" section table, and a "## Fixes" section table. Note that
// every model type used in the output must be registered (see task prompt).

public record FeatureData(string Title, string Author);
public record FixData(string Id, string Summary);
