using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Templates;

// A template is a human-authored document with {{placeholders}} for data.
// This demo loads real CVE data from cve.json and renders it through a template.

var jsonPath = Path.Combine(AppContext.BaseDirectory, "cve.json");
var templatePath = Path.Combine(AppContext.BaseDirectory, "template.md");

// Deserialize the CVE data (JSON uses snake_case naming)
var json = File.ReadAllText(jsonPath);
var cveData = JsonSerializer.Deserialize(json, CveJsonContext.Default.CveData)!;

// Load and configure the template
var template = MarkoutTemplate.Load(templatePath);

// Bind scalar values
var lastUpdated = DateTime.Parse(cveData.LastUpdated);
template.Bind("date", lastUpdated.ToString("MMMM yyyy"));
template.Bind("title", cveData.Title);

// Bind tables using the Markout context (uses snake_case binding names)
template.Bind(cveData, CveMarkoutContext.Default);

// Render to markdown
Console.WriteLine("=== Markdown ===");
Console.WriteLine(template.Render());

// Render to plain text
Console.WriteLine("=== Plain Text ===");
var plainWriter = new MarkoutWriter();
template.Render(plainWriter);
Console.WriteLine(plainWriter.ToString());

// --- Data types with both JSON and Markout attributes ---

[MarkoutSerializable(AutoFields = false)]
public record CveData
{
    [MarkoutIgnore]
    public string LastUpdated { get; init; } = "";

    [MarkoutIgnore]
    public string Title { get; init; } = "";

    [MarkoutSection(Name = "Vulnerabilities")]
    public List<CveDisclosure> Disclosures { get; init; } = [];

    [MarkoutSection(Name = "Affected Packages")]
    public List<CvePackage> Packages { get; init; } = [];
}

[MarkoutSerializable]
public record CveDisclosure
{
    [MarkoutPropertyName("CVE")]
    public string Id { get; init; } = "";

    public string Problem { get; init; } = "";

    [MarkoutIgnore]
    public CvssInfo Cvss { get; init; } = new();

    [JsonIgnore]
    [MarkoutPropertyName("Severity")]
    [MarkoutValueMap("HIGH=🟠", "CRITICAL=🔴", "MEDIUM=🟡", "LOW=🟢")]
    public string Severity => Cvss.Severity;

    [JsonIgnore]
    [MarkoutPropertyName("Score")]
    public double Score => Cvss.Score;
}

public record CvssInfo
{
    public double Score { get; init; }
    public string Severity { get; init; } = "";
}

[MarkoutSerializable]
public record CvePackage
{
    [MarkoutPropertyName("CVE")]
    public string CveId { get; init; } = "";

    [MarkoutPropertyName("Package")]
    public string Name { get; init; } = "";

    [MarkoutPropertyName(".NET")]
    public string Release { get; init; } = "";

    [MarkoutPropertyName("Fixed Version")]
    public string Fixed { get; init; } = "";
}

// JSON source generator context (uses snake_case naming policy)
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CveData))]
public partial class CveJsonContext : JsonSerializerContext
{
}

// Markout source generator context
[MarkoutContext(typeof(CveData))]
[MarkoutContext(typeof(CveDisclosure))]
[MarkoutContext(typeof(CvePackage))]
public partial class CveMarkoutContext : MarkoutSerializerContext
{
}
