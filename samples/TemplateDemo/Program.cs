using Markout;
using Markout.Templates;
using MarkdownTable.Formatting;

// A template is a human-authored document with {{placeholders}} for data.
// Like the source generator, templates render through the MarkoutWriter pipeline.
// See template.md alongside this file.

var templatePath = Path.Combine(AppContext.BaseDirectory, "template.md");
var template = MarkoutTemplate.Load(templatePath);

// Enable statistical table formatting for inline pipe tables
template.TableOptions = new TableFormatterOptions();

// Use PrettyTables so bound tables (via IMarkoutFormattable) also get aligned columns
var writerOptions = new MarkoutWriterOptions { PrettyTables = true };

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
Console.WriteLine(template.Render(writerOptions));

// Render through plain-text MarkoutWriter
Console.WriteLine("=== Plain Text ===");
var plainWriter = new MarkoutWriter();
template.Render(plainWriter);
Console.WriteLine(plainWriter.ToString());

// Now render WITHOUT commits (conditional section excluded)
Console.WriteLine("=== Markdown (no commits) ===");
var noCommits = MarkoutTemplate.Load(templatePath);
noCommits.TableOptions = new TableFormatterOptions();
noCommits.Bind("date", "February 2026");
noCommits.Bind("vuln-table", new VulnerabilityTable());
noCommits.Bind("product-table", new ProductTable());
// Don't bind "commits" — conditional section excluded
Console.WriteLine(noCommits.Render(writerOptions));

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
