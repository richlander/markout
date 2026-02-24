using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi.Spectre;
using Spectre.Console;

var username = args.FirstOrDefault(a => !a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    ?? RunCommand("gh", "api user --jq .login")
    ?? RunCommand("git", "config --get github.user");

if (username is null)
{
    Console.Write("GitHub username: ");
    username = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(username)) return;
}

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "Markout-GitHubActivity");

string profileJson, eventsJson;
try
{
    (profileJson, eventsJson) = await FetchAsync(http, username);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return;
}

var profile = JsonSerializer.Deserialize(profileJson, GitHubJsonContext.Default.GitHubUser)!;
var events = JsonSerializer.Deserialize(eventsJson, GitHubJsonContext.Default.ListGitHubEvent) ?? [];

var view = new ActivityView
{
    Title = profile.Login,
    Name = profile.Name ?? profile.Login,
    Location = profile.Location,
    PublicRepos = profile.PublicRepos,
    Followers = profile.Followers,
    Events = [.. events.Take(15).Select(e => new EventRow(
        FormatType(e.Type), e.Repo?.Name ?? "",
        DateTimeOffset.TryParse(e.CreatedAt, out var d) ? d.ToString("yyyy-MM-dd") : "",
        GetDetail(e)))]
};

MarkoutSerializer.Serialize(view, new SpectreWriter(AnsiConsole.Console), ActivityContext.Default);

// --- Helpers ---

static async Task<(string Profile, string Events)> FetchAsync(HttpClient http, string user)
{
    var p = http.GetStringAsync($"https://api.github.com/users/{user}");
    var e = http.GetStringAsync($"https://api.github.com/users/{user}/events/public");
    return (await p, await e);
}

static string FormatType(string t) => t switch
{
    "PushEvent" => "Push", "PullRequestEvent" => "PR", "IssuesEvent" => "Issue",
    "CreateEvent" => "Create", "DeleteEvent" => "Delete", "WatchEvent" => "Star",
    "ForkEvent" => "Fork", "IssueCommentEvent" => "Comment", "ReleaseEvent" => "Release",
    "PullRequestReviewEvent" => "Review", "GollumEvent" => "Wiki",
    _ => t.Replace("Event", "")
};

static string GetDetail(GitHubEvent e) => e.Type switch
{
    "PushEvent" => $"{e.Payload?.Size ?? 0} commit(s)",
    "PullRequestEvent" or "IssuesEvent" or "IssueCommentEvent" or "ReleaseEvent" => e.Payload?.Action ?? "",
    "CreateEvent" or "DeleteEvent" => e.Payload?.RefType ?? "",
    "ForkEvent" => "forked", "WatchEvent" => "starred", _ => ""
};

static string? RunCommand(string cmd, string args)
{
    try
    {
        using var p = Process.Start(new ProcessStartInfo(cmd, args)
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
        if (p is null) return null;
        var o = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return p.ExitCode == 0 && o.Length > 0 ? o : null;
    }
    catch { return null; }
}

// --- View Models ---

[MarkoutSerializable(TitleProperty = nameof(Title), NamingPolicy = NamingPolicy.PascalCaseWords)]
[MarkoutSkipNull]
public record ActivityView
{
    [MarkoutIgnore] public string Title { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Location { get; init; }
    [MarkoutPropertyName("Public repos")] public int PublicRepos { get; init; }
    public int Followers { get; init; }
    [MarkoutSection(Name = "Recent Activity")] public List<EventRow>? Events { get; init; }
}

[MarkoutSerializable]
public record EventRow(string Type, string Repository, string Date, string Detail);

[MarkoutContext(typeof(ActivityView))]
[MarkoutContext(typeof(EventRow))]
partial class ActivityContext : MarkoutSerializerContext;

// --- JSON Models ---

record GitHubUser(string Login, string? Name, string? Location, int PublicRepos, int Followers);
record GitHubEvent(string Type, GitHubRepo? Repo, string CreatedAt, EventPayload? Payload);
record GitHubRepo(string Name);
record EventPayload(string? Action, string? RefType, int? Size);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GitHubUser))]
[JsonSerializable(typeof(List<GitHubEvent>))]
internal partial class GitHubJsonContext : JsonSerializerContext;
