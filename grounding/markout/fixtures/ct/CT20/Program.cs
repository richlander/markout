// A security advisory tool produced this notice. Plain data — it carries no output formatting.

using Markout;

var advisory = new Advisory
{
    Title = "CVE-2024-1234 Advisory",
    Notice = new Callout(CalloutSeverity.Warning, "Upgrade to 13.0.3 or later."),
    Repro = new CodeSection("csharp", "var x = JsonConvert.DeserializeObject(input);"),
    Terms = new()
    {
        new Description("CVSS", "8.1", "High"),
        new Description("Vector", "network"),
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class Advisory
{
    public string Title { get; init; } = "";
    public Callout Notice { get; init; }
    public CodeSection Repro { get; init; }
    public List<Description> Terms { get; init; } = new();
}
