using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi.Spectre;
using Spectre.Console;

var username = args.FirstOrDefault(a => !a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    ?? GetGitHubUsername();

if (username is null)
{
    Console.Write("GitHub username: ");
    username = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(username))
        return;
}

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "Markout-GitHubActivity-Sample");

// Fetch user profile and recent events in parallel
var profileTask = http.GetStringAsync($"https://api.github.com/users/{username}");
var eventsTask = http.GetStringAsync($"https://api.github.com/users/{username}/events/public");

string profileJson, eventsJson;
try
{
    profileJson = await profileTask;
    eventsJson = await eventsTask;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Error fetching data for '{username}': {ex.Message}");
    return;
}

var profile = JsonSerializer.Deserialize(profileJson, GitHubJsonContext.Default.GitHubUser)!;
var events = JsonSerializer.Deserialize(eventsJson, GitHubJsonContext.Default.ListGitHubEvent) ?? [];

// Build view model
var view = new ActivityView
{
    Title = profile.Login,
    Name = profile.Name ?? profile.Login,
    Location = profile.Location,
    PublicRepos = profile.PublicRepos,
    Followers = profile.Followers,
    Events = events.Take(15).Select(e => new EventRow
    {
        Type = FormatEventType(e.Type),
        Repository = e.Repo?.Name ?? "",
        Date = DateTimeOffset.TryParse(e.CreatedAt, out var d) ? d.ToString("yyyy-MM-dd") : "",
        Detail = GetEventDetail(e)
    }).ToList()
};

// Render with SpectreWriter for rich ANSI terminal output
var writer = new SpectreWriter(AnsiConsole.Console);
MarkoutSerializer.Serialize(view, writer, ActivityContext.Default);

static string FormatEventType(string type) => type switch
{
    "PushEvent" => "Push",
    "PullRequestEvent" => "PR",
    "IssuesEvent" => "Issue",
    "CreateEvent" => "Create",
    "DeleteEvent" => "Delete",
    "WatchEvent" => "Star",
    "ForkEvent" => "Fork",
    "IssueCommentEvent" => "Comment",
    "PullRequestReviewEvent" => "Review",
    "PullRequestReviewCommentEvent" => "Review comment",
    "ReleaseEvent" => "Release",
    "CommitCommentEvent" => "Commit comment",
    "MemberEvent" => "Member",
    "PublicEvent" => "Public",
    "GollumEvent" => "Wiki",
    _ => type.Replace("Event", "")
};

static string GetEventDetail(GitHubEvent e) => e.Type switch
{
    "PushEvent" => $"{e.Payload?.Size ?? 0} commit(s)",
    "PullRequestEvent" => e.Payload?.Action ?? "",
    "IssuesEvent" => e.Payload?.Action ?? "",
    "CreateEvent" => e.Payload?.RefType ?? "",
    "DeleteEvent" => e.Payload?.RefType ?? "",
    "IssueCommentEvent" => e.Payload?.Action ?? "",
    "ForkEvent" => "forked",
    "WatchEvent" => "starred",
    "ReleaseEvent" => e.Payload?.Action ?? "",
    _ => ""
};

static string? GetGitHubUsername()
{
    // Try `gh` CLI first
    var name = RunCommand("gh", "api user --jq .login");
    if (name is not null) return name;

    // Fall back to git config
    return RunCommand("git", "config --get github.user");
}

static string? RunCommand(string command, string arguments)
{
    try
    {
        var psi = new ProcessStartInfo(command, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        if (proc is null) return null;
        var output = proc.StandardOutput.ReadToEnd().Trim();
        proc.WaitForExit();
        return proc.ExitCode == 0 && output.Length > 0 ? output : null;
    }
    catch
    {
        return null;
    }
}

// --- View Models ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ActivityView
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    public string Name { get; set; } = "";

    [MarkoutSkipNull]
    public string? Location { get; set; }

    [MarkoutPropertyName("Public repos")]
    public int PublicRepos { get; set; }

    public int Followers { get; set; }

    [MarkoutSection(Name = "Recent Activity")]
    public List<EventRow>? Events { get; set; }
}

[MarkoutSerializable]
public class EventRow
{
    public string Type { get; set; } = "";
    public string Repository { get; set; } = "";
    public string Date { get; set; } = "";
    public string Detail { get; set; } = "";
}

[MarkoutContext(typeof(ActivityView))]
[MarkoutContext(typeof(EventRow))]
public partial class ActivityContext : MarkoutSerializerContext { }

// --- JSON Models ---

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }

    [JsonPropertyName("followers")]
    public int Followers { get; set; }
}

public class GitHubEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("repo")]
    public GitHubRepo? Repo { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("payload")]
    public EventPayload? Payload { get; set; }
}

public class GitHubRepo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class EventPayload
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("ref_type")]
    public string? RefType { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }
}

[JsonSerializable(typeof(GitHubUser))]
[JsonSerializable(typeof(List<GitHubEvent>))]
internal partial class GitHubJsonContext : JsonSerializerContext { }
