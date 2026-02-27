#:project ../../src/Markout.Ansi.Spectre/Markout.Ansi.Spectre.csproj
#:package System.CommandLine@2.0.3

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi.Spectre;
using Markout.Formatting;
using Spectre.Console;

var repoArg = new Argument<string>("repo") { DefaultValueFactory = _ => "dotnet/runtime", Description = "GitHub repository (owner/repo)" };
var formatOption = new Option<string>("--format", "-f") { DefaultValueFactory = _ => "spectre", Description = "Output format" };
formatOption.AcceptOnlyFromAmong("spectre", "markdown", "oneline");
var sectionOption = new Option<string?>("--section", "-s") { Description = "Section to display" };

var rootCommand = new RootCommand("GitHub repository report — fields, charts, metrics, and tables via Markout")
{
    repoArg, formatOption, sectionOption
};

rootCommand.SetAction(Run);
return await rootCommand.Parse(args).InvokeAsync();

async Task Run(ParseResult parseResult, CancellationToken ct)
{
    var repo = parseResult.GetValue(repoArg)!;
    var format = parseResult.GetValue(formatOption)!;
    var section = parseResult.GetValue(sectionOption);

    var parts = repo.Split('/');
    if (parts.Length != 2) { Console.Error.WriteLine("Repository must be in owner/repo format."); return; }
    var (owner, name) = (parts[0], parts[1]);

    // Fetch and deserialize from GitHub API in parallel
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("User-Agent", "Markout-GitHubRepo");
    http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

    var repoTask = http.GetFromJsonAsync($"https://api.github.com/repos/{owner}/{name}", GitHubJsonContext.Default.RepoData, ct);
    var langsTask = http.GetFromJsonAsync($"https://api.github.com/repos/{owner}/{name}/languages", GitHubJsonContext.Default.DictionaryStringInt64, ct);
    var contribTask = http.GetFromJsonAsync($"https://api.github.com/repos/{owner}/{name}/contributors?per_page=10", GitHubJsonContext.Default.ListContributor, ct);
    var releasesTask = http.GetFromJsonAsync($"https://api.github.com/repos/{owner}/{name}/releases?per_page=10", GitHubJsonContext.Default.ListRelease, ct);

    var repoData = await repoTask;
    var languages = await langsTask ?? [];
    var contributors = await contribTask ?? [];
    var releases = await releasesTask ?? [];

    if (repoData is null)
    {
        Console.Error.WriteLine("Repository not found.");
        return;
    }

    // Choose renderer first — projection depends on writer capabilities
    var options = section is not null
        ? new MarkoutWriterOptions { IncludeSections = new HashSet<string> { section } }
        : new MarkoutWriterOptions();

    IMarkoutFormatter formatter = format switch
    {
        "markdown" => new MarkdownFormatter(),
        "oneline" => new OneLineFormatter(),
        _ => new SpectreWriter(AnsiConsole.Console),
    };

    var onelineOptions = format == "oneline"
        ? new MarkoutWriterOptions
        {
            IncludeDescription = false,
            IncludeSections = options.IncludeSections ?? new HashSet<string> { "Releases" }
        }
        : options;

    bool useMetrics = formatter is IMetricsFormatter;

    // Project to view model — shape selection depends on writer
    var totalBytes = languages.Values.Sum();
    int maxLanguages = 8;
    int maxContributors = 8;
    int maxReleases = 8;
    int maxPreviewReleases = 5;
    int maxReleaseNameLength = 40;

    var view = new RepoView
    {
        Title = repoData.FullName ?? repo,
        Description = repoData.Description ?? "",
        Stars = repoData.StargazersCount,
        Forks = repoData.ForksCount,
        OpenIssues = repoData.OpenIssuesCount,
        Language = repoData.Language ?? "unknown",
        License = repoData.License?.Name ?? "None",
        Created = DateTimeOffset.TryParse(repoData.CreatedAt, out var c) ? c.ToString("yyyy-MM-dd") : "",
        LastPush = DateTimeOffset.TryParse(repoData.PushedAt, out var p) ? p.ToString("yyyy-MM-dd") : "",
        ArchivedWarning = repoData.Archived
            ? new Callout(CalloutSeverity.Warning, "This repository has been archived and is read-only.")
            : default,
        NoLicenseWarning = repoData.License is null
            ? new Callout(CalloutSeverity.Note, "This repository does not specify a license.")
            : default,
        Languages = totalBytes > 0
            ? [new Breakdown("By bytes", languages
                .OrderByDescending(kv => kv.Value)
                .Take(maxLanguages)
                .Select(kv => new Segment(kv.Key, (int)(kv.Value * 100 / totalBytes)))
                .ToArray())]
            : null,
        ContributorMetrics = useMetrics ? contributors
            .Take(maxContributors)
            .Select(c => new Metric(c.Login ?? "unknown", c.Contributions))
            .ToList() : null,
        ContributorTable = !useMetrics ? contributors
            .Take(maxContributors)
            .Select(c => new ContributorRow
            {
                Login = c.Login ?? "unknown",
                Contributions = c.Contributions
            }).ToList() : null,
        Releases = releases
            .Where(r => !r.Prerelease)
            .Take(maxReleases)
            .Select(r => new ReleaseRow
            {
                Tag = r.TagName ?? "",
                Name = Truncate(r.Name ?? r.TagName ?? "", maxReleaseNameLength),
                Published = DateTimeOffset.TryParse(r.PublishedAt, out var d) ? d.ToString("yyyy-MM-dd") : "",
            }).ToList(),
        PreviewReleases = releases
            .Where(r => r.Prerelease)
            .Take(maxPreviewReleases)
            .Select(r => new ReleaseRow
            {
                Tag = r.TagName ?? "",
                Name = Truncate(r.Name ?? r.TagName ?? "", maxReleaseNameLength),
                Published = DateTimeOffset.TryParse(r.PublishedAt, out var d) ? d.ToString("yyyy-MM-dd") : "",
            }).ToList()
    };

    MarkoutSerializer.Serialize(view, Console.Out, formatter, RepoContext.Default, onelineOptions);
}

static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

// --- View Models ---

[MarkoutSerializable(TitleProperty = nameof(Title), DescriptionProperty = nameof(Description))]
public class RepoView
{
    public string Title { get; set; } = "";

    [MarkoutIgnore]
    public string Description { get; set; } = "";

    [MarkoutDisplayFormat("{0:N0}")]
    public int Stars { get; set; }

    [MarkoutDisplayFormat("{0:N0}")]
    public int Forks { get; set; }

    [MarkoutPropertyName("Open Issues")]
    [MarkoutDisplayFormat("{0:N0}")]
    public int OpenIssues { get; set; }

    public string Language { get; set; } = "";
    public string License { get; set; } = "";
    public string Created { get; set; } = "";

    [MarkoutPropertyName("Last Push")]
    public string LastPush { get; set; } = "";

    [MarkoutIgnoreInTable]
    [MarkoutSkipDefault]
    public Callout ArchivedWarning { get; set; }

    [MarkoutIgnoreInTable]
    [MarkoutSkipDefault]
    public Callout NoLicenseWarning { get; set; }

    [MarkoutSection(Name = "Languages")]
    [MarkoutIgnoreInTable]
    public List<Breakdown>? Languages { get; set; }

    [MarkoutSection(Name = "Top Contributors")]
    [MarkoutIgnoreInTable]
    public List<Metric>? ContributorMetrics { get; set; }

    [MarkoutSection(Name = "Top Contributors")]
    public List<ContributorRow>? ContributorTable { get; set; }

    [MarkoutSection(Name = "Releases")]
    public List<ReleaseRow>? Releases { get; set; }

    [MarkoutSection(Name = "Preview Releases")]
    public List<ReleaseRow>? PreviewReleases { get; set; }
}

[MarkoutSerializable]
public class ContributorRow
{
    public string Login { get; set; } = "";
    [MarkoutDisplayFormat("{0:N0}")]
    public int Contributions { get; set; }
}

[MarkoutSerializable]
public class ReleaseRow
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Published { get; set; } = "";
}

[MarkoutContext(typeof(RepoView))]
[MarkoutContext(typeof(ContributorRow))]
[MarkoutContext(typeof(ReleaseRow))]
public partial class RepoContext : MarkoutSerializerContext { }

// --- JSON Models ---

public class RepoData
{
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public string? Language { get; set; }
    public LicenseInfo? License { get; set; }
    public bool Archived { get; set; }
    public string? CreatedAt { get; set; }
    public string? PushedAt { get; set; }
}

public class LicenseInfo
{
    public string? Key { get; set; }
    public string? Name { get; set; }
}

public class Contributor
{
    public string? Login { get; set; }
    public int Contributions { get; set; }
}

public class Release
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? PublishedAt { get; set; }
    public bool Prerelease { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(RepoData))]
[JsonSerializable(typeof(Dictionary<string, long>))]
[JsonSerializable(typeof(List<Contributor>))]
[JsonSerializable(typeof(List<Release>))]
internal partial class GitHubJsonContext : JsonSerializerContext { }
