using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi.Spectre;
using Spectre.Console;
using TreeNode = Markout.TreeNode;

var cutoff = DateTimeOffset.UtcNow.AddMonths(-6);

// Fetch the release index
using var http = new HttpClient();
var indexJson = await http.GetStringAsync(
    "https://github.com/dotnet/core/raw/release-index/release-notes/index.json");
var index = JsonSerializer.Deserialize(indexJson, CveJsonContext.Default.ReleaseIndex)!;

var supported = index.Embedded?.Releases?
    .Where(r => r.Supported)
    .ToList() ?? [];

// Fetch each version index in parallel
var versionData = await Task.WhenAll(supported.Select(async r =>
{
    var url = r.Links?.Self?.Href;
    if (url is null) return (r.Version, (VersionIndex?)null);
    var json = await http.GetStringAsync(url);
    return (r.Version, JsonSerializer.Deserialize(json, CveJsonContext.Default.VersionIndex));
}));

// Collect unique CVE JSON URLs from security patches within the span
var cveUrls = new HashSet<string>();
foreach (var (_, vi) in versionData)
{
    foreach (var patch in vi?.Embedded?.Patches ?? [])
    {
        if (!patch.Security) continue;
        if (!DateTimeOffset.TryParse(patch.Date, out var d) || d < cutoff) continue;
        if (patch.Links?.CveJson?.Href is { } url)
            cveUrls.Add(url);
    }
}

// Fetch all CVE data in parallel
var cveDataList = (await Task.WhenAll(cveUrls.Select(async url =>
{
    var json = await http.GetStringAsync(url);
    return JsonSerializer.Deserialize(json, CveJsonContext.Default.CveData);
}))).Where(c => c is not null).ToList();

// Build CVE ID → (reference URL, severity) lookup
var cveInfo = new Dictionary<string, (string Url, string? Severity)>();
foreach (var d in cveDataList.SelectMany(c => c!.Disclosures ?? []))
    cveInfo.TryAdd(d.Id, (d.References?.FirstOrDefault() ?? "", d.Cvss?.Severity));

// Build tree nodes
var tree = new List<TreeNode>();
foreach (var (version, vi) in versionData.OrderByDescending(v => decimal.TryParse(v.Version, out var n) ? n : 0))
{
    if (vi is null) continue;
    var date = DateTimeOffset.TryParse(vi.LatestPatchDate, out var d)
        ? d.ToString("yyyy-MM-dd") : "unknown";

    // Collect unique CVEs for this version across all fetched CVE data
    var cves = cveDataList
        .SelectMany(c => c!.ReleaseCves?.TryGetValue(version, out var ids) == true ? ids : [])
        .Distinct()
        .OrderBy(id => id)
        .Select(id =>
        {
            var (url, severity) = cveInfo.TryGetValue(id, out var info) ? info : ("", null);
            var label = !string.IsNullOrEmpty(url) ? $"[{id}]({url})" : id;
            var badge = severity?.ToUpperInvariant() switch
            {
                "CRITICAL" => "🔴",
                "HIGH" => "🟠",
                "MEDIUM" => "🟡",
                "LOW" => "🟢",
                _ => null
            };
            return new TreeNode(label, badge);
        })
        .ToList();

    if (cves.Count == 0)
        cves.Add(new TreeNode("None"));

    tree.Add(new TreeNode($"{version} (last updated: {date})", cves));
}

// Count severity distribution
var severityCounts = cveInfo.Values
    .GroupBy(v => v.Severity?.ToUpperInvariant() ?? "UNKNOWN")
    .OrderByDescending(g => g.Key switch { "CRITICAL" => 0, "HIGH" => 1, "MEDIUM" => 2, "LOW" => 3, _ => 4 })
    .ToDictionary(g => g.Key, g => g.Count());
var criticalCount = severityCounts.GetValueOrDefault("CRITICAL");

// Serialize to markdown
var view = new LatestCvesView
{
    Title = ".NET Security Advisories",
    Span = $"{cutoff:MMM yyyy} \u2013 {DateTimeOffset.UtcNow:MMM yyyy}",
    CriticalWarning = criticalCount > 0
        ? new Callout(CalloutSeverity.Caution, $"{criticalCount} critical severity CVE(s) found. Update affected runtimes immediately.")
        : default,
    SeverityBreakdown = severityCounts.Count > 0
        ? [new Breakdown("By Severity", severityCounts.Select(kv => new Segment(kv.Key, kv.Value)).ToArray())]
        : null,
    Releases = tree
};
MarkoutSerializer.Serialize(view, Console.Out, new SpectreWriter(AnsiConsole.Console), LatestCvesContext.Default);

// --- View Model ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class LatestCvesView
{
    public string Title { get; set; } = "";

    public string Span { get; set; } = "";

    [MarkoutIgnoreInTable]
    [MarkoutSkipDefault]
    public Callout CriticalWarning { get; set; }

    [MarkoutSection(Name = "Severity Distribution")]
    [MarkoutIgnoreInTable]
    public IReadOnlyList<Breakdown>? SeverityBreakdown { get; set; }

    [MarkoutIgnoreInTable]
    public List<TreeNode>? Releases { get; set; }
}

[MarkoutContext(typeof(LatestCvesView))]
public partial class LatestCvesContext : MarkoutSerializerContext { }

// --- JSON Models (snake_case via options; explicit for HAL _embedded/_links) ---

public class ReleaseIndex
{
    [JsonPropertyName("_embedded")]
    public ReleaseEmbedded? Embedded { get; set; }
}

public class ReleaseEmbedded
{
    public List<ReleaseEntry>? Releases { get; set; }
}

public class ReleaseEntry
{
    public string Version { get; set; } = "";
    public bool Supported { get; set; }

    [JsonPropertyName("_links")]
    public EntryLinks? Links { get; set; }
}

public class EntryLinks
{
    public LinkRef? Self { get; set; }
}

public class LinkRef
{
    public string Href { get; set; } = "";
}

public class VersionIndex
{
    public string? LatestPatchDate { get; set; }

    [JsonPropertyName("_embedded")]
    public PatchEmbedded? Embedded { get; set; }
}

public class PatchEmbedded
{
    public List<PatchRelease>? Patches { get; set; }
}

public class PatchRelease
{
    public string? Date { get; set; }
    public bool Security { get; set; }

    [JsonPropertyName("_links")]
    public PatchLinks? Links { get; set; }
}

public class PatchLinks
{
    [JsonPropertyName("cve-json")]
    public LinkRef? CveJson { get; set; }
}

public class CveData
{
    public List<CveDisclosure>? Disclosures { get; set; }
    public Dictionary<string, List<string>>? ReleaseCves { get; set; }
}

public class CveDisclosure
{
    public string Id { get; set; } = "";
    public List<string>? References { get; set; }
    public CveCvss? Cvss { get; set; }
}

public class CveCvss
{
    public string? Severity { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ReleaseIndex))]
[JsonSerializable(typeof(VersionIndex))]
[JsonSerializable(typeof(CveData))]
internal partial class CveJsonContext : JsonSerializerContext { }
