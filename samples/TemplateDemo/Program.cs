using Markout;
using Markout.Templates;

// A template is a human-authored document with {{placeholders}} for data.
// It's a peer entry path to the source generator — both render through MarkoutWriter.

var template = MarkoutTemplate.Parse("""
    # .NET Security Report for {{date}}

    The following vulnerabilities were disclosed this month.

    {{vuln-table}}

    ## Affected Products

    {{product-table}}

    {{#if commits}}
    ## Related Commits

    The following commits address the vulnerabilities above.

    {{commit-table}}
    {{/if}}
    """);

// Bind inline values
template.Bind("date", "February 2026");

// Bind IMarkoutFormattable objects for block-level shape rendering
template.Bind("vuln-table", new VulnerabilityTable());
template.Bind("product-table", new ProductTable());

// Bind a truthy value to include the conditional section
template.Bind("commits", "yes");
template.Bind("commit-table", new CommitTable());

// Render through MarkdownWriter (default)
Console.WriteLine("=== Markdown ===");
Console.WriteLine(template.Render());

// Render through plain-text MarkoutWriter
Console.WriteLine("=== Plain Text ===");
var plainWriter = new MarkoutWriter();
template.Render(plainWriter);
Console.WriteLine(plainWriter.ToString());

// Now render WITHOUT commits (conditional section excluded)
Console.WriteLine("=== Markdown (no commits) ===");
var noCommits = MarkoutTemplate.Parse("""
    # .NET Security Report for {{date}}

    {{vuln-table}}

    {{#if commits}}
    ## Related Commits

    {{commit-table}}
    {{/if}}
    """);
noCommits.Bind("date", "February 2026");
noCommits.Bind("vuln-table", new VulnerabilityTable());
// Don't bind "commits" — section excluded
Console.WriteLine(noCommits.Render());

// --- Formattable types that render through the writer shape system ---

class VulnerabilityTable : IMarkoutFormattable
{
    public void WriteTo(MarkoutWriter writer)
    {
        writer.WriteTableStart("CVE", "Severity", "Component");
        writer.WriteTableRow("CVE-2026-1234", "Critical", "System.Net.Http");
        writer.WriteTableRow("CVE-2026-1235", "High", "Microsoft.Data.SqlClient");
        writer.WriteTableRow("CVE-2026-1236", "Medium", "System.Text.Json");
        writer.WriteTableEnd();
    }

    public string? ToMarkoutString() => "3 vulnerabilities";
}

class ProductTable : IMarkoutFormattable
{
    public void WriteTo(MarkoutWriter writer)
    {
        writer.WriteTableStart("Product", "Version", "Status");
        writer.WriteTableRow(".NET 9.0", "9.0.4", "Patched");
        writer.WriteTableRow(".NET 8.0", "8.0.12", "Patched");
        writer.WriteTableEnd();
    }

    public string? ToMarkoutString() => "2 products";
}

class CommitTable : IMarkoutFormattable
{
    public void WriteTo(MarkoutWriter writer)
    {
        writer.WriteTableStart("SHA", "Message", "Author");
        writer.WriteTableRow("abc1234", "Fix HTTP header injection", "security-bot");
        writer.WriteTableRow("def5678", "Patch SQL parameter handling", "security-bot");
        writer.WriteTableEnd();
    }

    public string? ToMarkoutString() => "2 commits";
}
