// A build outcome. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Build";
var hasErrors = false;

var errors = new List<string> { "boom" };

// TODO: print a GitHub-flavored Markdown report: an H1 title. Show an "## Errors" section ONLY
// when hasErrors is true — here it is false, so the section must NOT appear — using the
// section's show-when option. See task prompt.
