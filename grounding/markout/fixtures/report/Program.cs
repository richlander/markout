// A dependency-scan produced this data. Render it as a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var scanned = 42;
var vulnerable = 2;

var advisories = new List<AdvisoryData>
{
    new("Contoso.Data", "Critical", "CVE-2025-0001"),
    new("Contoso.Web", "High", "CVE-2025-0002"),
};

// TODO: print a GitHub-flavored Markdown report to the console (see task prompt).

public record AdvisoryData(string Package, string Severity, string Cve);
