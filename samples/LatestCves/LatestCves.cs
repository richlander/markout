using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi.Spectre;
using Spectre.Console;

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

// Build CVE ID → view model lookup
var cveItems = cveDataList
    .SelectMany(c => c!.Disclosures ?? [])
    .DistinctBy(d => d.Id)
    .ToDictionary(
        d => d.Id,
        d => new CveItemView
        {
            Id = d.Id,
            Url = d.References?.FirstOrDefault(),
            Severity = d.Cvss?.Severity
        });

// Build release views with CVE items
var releases = versionData
    .OrderByDescending(v => decimal.TryParse(v.Version, out var n) ? n : 0)
    .Where(v => v.Item2 is not null)
    .Select(v =>
    {
        var (version, vi) = v;
        var lastUpdated = DateTimeOffset.TryParse(vi!.LatestPatchDate, out var d)
            ? d.ToString("yyyy-MM-dd") : "unknown";

        var cves = cveDataList
            .SelectMany(c => c!.ReleaseCves?.TryGetValue(version, out var ids) == true ? ids : [])
            .Distinct()
            .OrderBy(id => id)
            .Select(id => cveItems.TryGetValue(id, out var item) ? item : new CveItemView { Id = id })
            .ToList();

        return new ReleaseView
        {
            Version = version,
            LastUpdated = lastUpdated,
            Cves = cves
        };
    })
    .ToList();

// Count severity distribution from view models
var severityCounts = cveItems.Values
    .GroupBy(v => v.Severity?.ToUpperInvariant() ?? "UNKNOWN")
    .OrderByDescending(g => g.Key switch { "CRITICAL" => 0, "HIGH" => 1, "MEDIUM" => 2, "LOW" => 3, _ => 4 })
    .ToDictionary(g => g.Key, g => g.Count());
var criticalCount = severityCounts.GetValueOrDefault("CRITICAL");

// Build the view model
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
    Releases = releases
};

var writer = new SpectreWriter(AnsiConsole.Console);
MarkoutSerializer.Serialize(view, writer, LatestCvesContext.Default);

// --- View Models ---

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

    [MarkoutSection(Name = "Releases")]
    [MarkoutIgnoreInTable]
    public List<ReleaseView>? Releases { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(VersionLabel), AutoFields = false)]
public class ReleaseView
{
    [MarkoutIgnore]
    public string Version { get; set; } = "";

    [MarkoutIgnore]
    public string LastUpdated { get; set; } = "";

    [MarkoutIgnore]
    public string VersionLabel => $".NET {Version} (last updated: {LastUpdated})";

    [MarkoutSection(Name = "CVEs")]
    public List<CveItemView>? Cves { get; set; }
}

[MarkoutSerializable]
public class CveItemView
{
    [MarkoutPropertyName("CVE")]
    [MarkoutLink(TextProperty = nameof(Id))]
    public string? Url { get; set; }

    [MarkoutIgnore]
    public string Id { get; set; } = "";

    [MarkoutValueMap("CRITICAL=🔴", "HIGH=🟠", "MEDIUM=🟡", "LOW=🟢")]
    public string? Severity { get; set; }
}

[MarkoutContext(typeof(LatestCvesView))]
[MarkoutContext(typeof(ReleaseView))]
[MarkoutContext(typeof(CveItemView))]
public partial class LatestCvesContext : MarkoutSerializerContext { }

// --- JSON Models (snake_case naming via options) ---

public class ReleaseIndex
{
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
