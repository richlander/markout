// A release note lists breaking changes. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Release v2.0";

var changes = new List<BreakingChange>
{
    new("API", "removed Foo()"),
    new("Config", "renamed timeout to timeoutMs"),
};

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title, then the
// changes under a "Breaking Changes" subheading rendered at heading LEVEL 3 (### , not ##)
// as a table. Use the library's section attribute to set the level (see task prompt).

public record BreakingChange(string Area, string Detail);
