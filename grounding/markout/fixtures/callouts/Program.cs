// A deployment produced these advisories at different severities. Render a report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Deploy Status";
var warning = "Disk space low";
var caution = "Cert expires in 5 days";
var note = "Rolled to 3 of 5 nodes";

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title, then three
// alert callouts carrying the messages above — a Warning, a Caution, and a Note — using
// the library's built-in callout shape (do not hand-write the alert syntax). See task prompt.
