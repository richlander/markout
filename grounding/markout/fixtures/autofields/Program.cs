// A scan produced metadata and findings. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Audit";
var generatedBy = "ci-bot";   // metadata that should NOT appear as a field

var findings = new List<Finding>
{
    new("CVE-2025-0001", "High"),
};

// TODO: print a GitHub-flavored Markdown report: an H1 title and a "## Findings" table.
// Do NOT render the scalar metadata (generatedBy) as a field table — suppress auto fields
// so only the section renders (use the library's serializable option). See task prompt.

public record Finding(string Id, string Severity);
