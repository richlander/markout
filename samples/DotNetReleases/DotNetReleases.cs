using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;

// Fetch the .NET release index
using var http = new HttpClient();
var json = await http.GetStringAsync(
    "https://github.com/dotnet/core/raw/release-index/release-notes/index.json");

var index = JsonSerializer.Deserialize(json, ReleasesJsonContext.Default.ReleaseIndex)!;

// Project to view model
var releases = index.Embedded?.Releases ?? [];
var unsupported = releases.Where(r => !r.Supported).ToList();

var view = new ReleasesView
{
    Title = index.Title ?? ".NET Releases",
    LatestMajor = index.LatestMajor,
    LatestLtsMajor = index.LatestLtsMajor,
    EolWarning = unsupported.Count > 0
        ? new Callout(CalloutSeverity.Warning, $"{unsupported.Count} release(s) are end-of-life and no longer receive security patches.")
        : default,
    TypeBreakdown = [new Breakdown("Release Types", releases
        .GroupBy(r => r.ReleaseType?.ToUpperInvariant() ?? "UNKNOWN")
        .OrderByDescending(g => g.Count())
        .Select(g => new Segment(g.Key, g.Count()))
        .ToArray())],
    Releases = releases.Select(r => new ReleaseRow
    {
        Version = r.Version,
        Type = r.ReleaseType?.ToUpperInvariant() ?? "",
        Supported = r.Supported
    }).ToList()
};

// Serialize to markdown
MarkoutSerializer.Serialize(view, Console.Out, ReleasesContext.Default);

// --- Models ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ReleasesView
{
    public string Title { get; set; } = "";

    [MarkoutPropertyName("Latest Major")]
    [MarkoutSkipNull]
    public string? LatestMajor { get; set; }

    [MarkoutPropertyName("Latest LTS")]
    [MarkoutSkipNull]
    public string? LatestLtsMajor { get; set; }

    [MarkoutIgnoreInTable]
    [MarkoutSkipDefault]
    public Callout EolWarning { get; set; }

    [MarkoutSection(Name = "Type Distribution")]
    [MarkoutIgnoreInTable]
    public IReadOnlyList<Breakdown>? TypeBreakdown { get; set; }

    [MarkoutSection(Name = "All Releases")]
    public List<ReleaseRow>? Releases { get; set; }
}

[MarkoutSerializable]
public class ReleaseRow
{
    public string Version { get; set; } = "";
    public string Type { get; set; } = "";
    [MarkoutBoolFormat("✓", "✗")]
    public bool Supported { get; set; }
}

[MarkoutContext(typeof(ReleasesView))]
[MarkoutContext(typeof(ReleaseRow))]
public partial class ReleasesContext : MarkoutSerializerContext { }

// --- JSON Models ---

public class ReleaseIndex
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("latest_major")]
    public string? LatestMajor { get; set; }

    [JsonPropertyName("latest_lts_major")]
    public string? LatestLtsMajor { get; set; }

    [JsonPropertyName("_embedded")]
    public EmbeddedReleases? Embedded { get; set; }
}

public class EmbeddedReleases
{
    [JsonPropertyName("releases")]
    public List<Release>? Releases { get; set; }
}

public class Release
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("release_type")]
    public string? ReleaseType { get; set; }

    [JsonPropertyName("supported")]
    public bool Supported { get; set; }
}

[JsonSerializable(typeof(ReleaseIndex))]
internal partial class ReleasesJsonContext : JsonSerializerContext { }
