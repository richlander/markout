// CanCon. It's the law!

using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi;
using Markout.Ansi.Spectre;
using Microsoft.Extensions.Terminal;
using Spectre.Console;
using TreeNode = Markout.TreeNode;

// Load data from JSON files
var basePath = AppContext.BaseDirectory;
var actorsJson = await File.ReadAllTextAsync(Path.Combine(basePath, "actors.json"));
var showsJson = await File.ReadAllTextAsync(Path.Combine(basePath, "shows.json"));

var actors = JsonSerializer.Deserialize(actorsJson, CanConJsonContext.Default.ListActor)!;
var shows = JsonSerializer.Deserialize(showsJson, CanConJsonContext.Default.ListShow)!;

var cityCountry = new Dictionary<string, string>
{
    ["Toronto"] = "Canada",
    ["Vancouver"] = "Canada",
    ["Ontario"] = "Canada",
    ["London"] = "England",
    ["Sydney"] = "Australia",
    ["South Carolina"] = "USA",
    ["Budapest"] = "Hungary",
    ["Los Angeles"] = "USA",
};

// Parse arguments: [--format markdown|ansi|plain] [-n count] [query]
var argList = args.ToList();
var format = "markdown";
int? maxItems = null;

var formatIndex = argList.IndexOf("--format");
if (formatIndex >= 0 && formatIndex + 1 < argList.Count)
{
    format = argList[formatIndex + 1].ToLowerInvariant();
    argList.RemoveRange(formatIndex, 2);
}
else if (argList.Remove("--ansi"))
{
    format = "ansi";
}
else if (argList.Remove("--plain"))
{
    format = "plain";
}
else if (argList.Remove("--oneline"))
{
    format = "oneline";
}
else if (argList.Remove("--diagram"))
{
    format = "diagram";
}
else if (argList.Remove("--spectre"))
{
    format = "spectre";
}

var nIndex = argList.IndexOf("-n");
if (nIndex >= 0 && nIndex + 1 < argList.Count && int.TryParse(argList[nIndex + 1], out var n))
{
    maxItems = n;
    argList.RemoveRange(nIndex, 2);
}

// Section filters for summary command
HashSet<string>? includeSections = null;
if (argList.Remove("--actors")) (includeSections ??= []).Add("Actors");
if (argList.Remove("--shows")) (includeSections ??= []).Add("Shows");
if (argList.Remove("--cities")) (includeSections ??= []).Add("Filming Locations");

var query = argList.Count > 0 ? string.Join(" ", argList).ToLowerInvariant() : "";

if (query is "" or "-h" or "--help" or "help")
{
    Console.WriteLine("""
        Canadian Content Database - CanCon. It's the law!

        Usage: dotnet run -- [--format markdown|ansi|spectre|plain|oneline|diagram] [--ansi] [--spectre] [--plain] [--oneline] [--diagram] [-n count] [query]

        Formats:
          markdown      Markdown output (default)
          ansi          ANSI terminal output with colors
          spectre       ANSI terminal output via Spectre.Console
          plain         Plain text output
          oneline       Compact columnar tables only
          diagram       Tree/diagram output only

        Options:
          -n count      Limit table rows to count (e.g. -n 5)
          --actors      Filter summary to actors table
          --shows       Filter summary to shows table
          --cities      Filter summary to filming locations table

        Queries:
          summary       Show all actors, shows, and filming locations
          ryan          Actors named Ryan (Gosling, Reynolds)
          rachel        Actors named Rachel (McAdams, Skarsten)
          gosling       Ryan Gosling's filmography
          reynolds      Ryan Reynolds' filmography
          expanse       The Expanse with Canadian cast
          schitt        Schitt's Creek with Canadian cast
          toronto       Shows filmed in Toronto
          vancouver     Shows filmed in Vancouver
          tree          Filmography trees (shows grouped by filming location)
          bars          Bar chart of shows per filming city
          vbars         Vertical bar chart of shows per filming city

        Examples:
          dotnet run -- summary
          dotnet run -- ryan
          dotnet run -- --format ansi toronto
          dotnet run -- --format plain tree
          dotnet run -- --format ansi bars
          dotnet run -- --oneline summary --actors
        """);
    return;
}

// Create the writer for the selected format
var options = new MarkoutWriterOptions { MaxItems = maxItems, IncludeSections = includeSections };
MarkoutWriter writer = format switch
{
    "ansi" => new AnsiWriter(new AnsiTerminal(new SystemConsole()), options),
    "spectre" => new SpectreWriter(AnsiConsole.Console, options),
    "plain" => new MarkoutWriter(Console.Out, options),
    "oneline" => new OneLineWriter(Console.Out, options),
    "diagram" => new DiagramWriter(Console.Out, options),
    _ => new MarkdownWriter(Console.Out, options),
};

if (query.Contains("summary"))
{
    if (format == "oneline" && includeSections == null)
    {
        Console.Error.WriteLine("oneline format requires a section flag with summary: --actors, --shows, or --cities");
        return;
    }
    // Show all actors and shows
    var overview = new CanConOverview
    {
        Title = "Canadian Content Database",
        Actors = actors.Select(a => new ActorRow
        {
            Name = a.Name,
            Birthplace = a.Birthplace,
            BirthYear = a.BirthYear,
            Citizenship = string.Join(", ", a.Citizenship)
        }).ToList(),
        Shows = shows.Select(s => new ShowRow
        {
            Title = s.Title,
            Type = s.Type,
            Years = s.Years,
            FilmingLocation = s.Location
        }).ToList(),
        Cities = shows
            .GroupBy(s => s.Location)
            .OrderByDescending(g => g.Count())
            .Select(g =>
            {
                var mostRecent = g.OrderByDescending(s => s.Years).First();
                return new CityRow
                {
                    City = g.Key,
                    Country = cityCountry.GetValueOrDefault(g.Key, ""),
                    ShowCount = g.Count(),
                    MostRecent = $"{mostRecent.Title} ({mostRecent.Years})"
                };
            }).ToList()
    };
    MarkoutSerializer.Serialize(overview, writer, CanConContext.Default);
}
else if (query.Contains("ryan") || query.Contains("rachel"))
{
    // Filter actors by first name
    var nameFilter = query.Contains("ryan") ? "Ryan" : "Rachel";
    var filtered = actors
        .Where(a => a.Name.StartsWith(nameFilter, StringComparison.OrdinalIgnoreCase))
        .ToList();

    var view = new ActorSearchResult
    {
        Title = $"Actors Named {nameFilter}",
        Results = filtered.Select(a => new ActorDetailRow
        {
            Name = a.Name,
            Birthplace = a.Birthplace,
            BirthYear = a.BirthYear,
            Citizenship = string.Join(", ", a.Citizenship),
            KnownFor = string.Join(", ", a.Shows.Take(3))
        }).ToList()
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else if (query.Contains("expanse"))
{
    var show = shows.First(s => s.Title.Contains("Expanse", StringComparison.OrdinalIgnoreCase));
    var castActors = actors.Where(a => show.CanadianActors.Contains(a.Name)).ToList();

    var view = new ShowDetailView
    {
        Title = show.Title,
        Type = show.Type,
        Years = show.Years,
        FilmingLocation = show.Location,
        Cast = castActors.Select(a => new ActorRow
        {
            Name = a.Name,
            Birthplace = a.Birthplace,
            BirthYear = a.BirthYear,
            Citizenship = string.Join(", ", a.Citizenship)
        }).ToList()
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else if (query.Contains("schitt"))
{
    var show = shows.First(s => s.Title.Contains("Schitt", StringComparison.OrdinalIgnoreCase));
    var castActors = actors.Where(a => show.CanadianActors.Contains(a.Name)).ToList();

    var view = new ShowDetailView
    {
        Title = show.Title,
        Type = show.Type,
        Years = show.Years,
        FilmingLocation = show.Location,
        Cast = castActors.Select(a => new ActorRow
        {
            Name = a.Name,
            Birthplace = a.Birthplace,
            BirthYear = a.BirthYear,
            Citizenship = string.Join(", ", a.Citizenship)
        }).ToList()
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else if (query.Contains("toronto") || query.Contains("vancouver"))
{
    var city = query.Contains("toronto") ? "Toronto" : "Vancouver";
    var filtered = shows.Where(s => s.Location.Contains(city, StringComparison.OrdinalIgnoreCase)).ToList();

    var view = new LocationSearchResult
    {
        Title = $"Shows Filmed in {city}",
        Results = filtered.Select(s => new ShowRow
        {
            Title = s.Title,
            Type = s.Type,
            Years = s.Years,
            FilmingLocation = s.Location
        }).ToList()
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else if (query.Contains("gosling") || query.Contains("reynolds"))
{
    var lastName = query.Contains("gosling") ? "Gosling" : "Reynolds";
    var actor = actors.First(a => a.Name.Contains(lastName, StringComparison.OrdinalIgnoreCase));
    var actorShows = shows.Where(s => s.CanadianActors.Contains(actor.Name)).ToList();

    var view = new ActorFilmography
    {
        Name = actor.Name,
        Birthplace = actor.Birthplace,
        BirthYear = actor.BirthYear,
        Citizenship = string.Join(", ", actor.Citizenship),
        Filmography = actorShows.Select(s => new ShowRow
        {
            Title = s.Title,
            Type = s.Type,
            Years = s.Years,
            FilmingLocation = s.Location
        }).ToList()
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else if (query.Contains("tree"))
{
    var view = new FilmographyTreeView
    {
        Title = "Canadian Content — Filmography Trees",
        Filmography = actors
            .Select(actor =>
            {
                var actorShows = shows
                    .Where(s => s.CanadianActors.Contains(actor.Name))
                    .ToList();

                if (actorShows.Count == 0) return null;

                var cityNodes = actorShows
                    .GroupBy(s => s.Location)
                    .OrderBy(g => g.Key)
                    .Select(g => new TreeNode(
                        g.Key,
                        g.Select(s => $"{s.Title} ({s.Years})")))
                    .ToList();

                return new TreeNode(actor.Name, cityNodes);
            })
            .Where(n => n is not null)
            .ToList()!
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else if (query.Contains("vbars"))
{
    var bars = shows
        .GroupBy(s => s.Location)
        .OrderByDescending(g => g.Count())
        .Select(g => new BarItem(g.Key, g.Count()))
        .ToList();
    writer.WriteHeading(1, "Shows per Filming Location");
    writer.WriteVerticalBarChart(bars);
}
else if (query.Contains("bars"))
{
    var view = new ShowsByLocationChart
    {
        Title = "Shows per Filming Location",
        Bars = shows
            .GroupBy(s => s.Location)
            .OrderByDescending(g => g.Count())
            .Select(g => new BarItem(g.Key, g.Count()))
            .ToList()
    };
    MarkoutSerializer.Serialize(view, writer, CanConContext.Default);
}
else
{
    Console.Error.WriteLine($"Unknown query: {query}");
    Console.Error.WriteLine("Try: summary, ryan, rachel, gosling, reynolds, expanse, schitt, toronto, vancouver, tree, bars, vbars");
}

// --- JSON Models ---

public class Actor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("birthplace")]
    public string Birthplace { get; set; } = "";

    [JsonPropertyName("birthYear")]
    public int BirthYear { get; set; }

    [JsonPropertyName("citizenship")]
    public List<string> Citizenship { get; set; } = new();

    [JsonPropertyName("shows")]
    public List<string> Shows { get; set; } = new();
}

public class Show
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("years")]
    public string Years { get; set; } = "";

    [JsonPropertyName("location")]
    public string Location { get; set; } = "";

    [JsonPropertyName("canadianActors")]
    public List<string> CanadianActors { get; set; } = new();
}

[JsonSerializable(typeof(List<Actor>))]
[JsonSerializable(typeof(List<Show>))]
internal partial class CanConJsonContext : JsonSerializerContext { }

// --- Markout View Models ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CanConOverview
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Actors")]
    public List<ActorRow>? Actors { get; set; }

    [MarkoutSection(Name = "Shows")]
    public List<ShowRow>? Shows { get; set; }

    [MarkoutSection(Name = "Filming Locations")]
    public List<CityRow>? Cities { get; set; }
}

[MarkoutSerializable]
public class ActorRow
{
    public string Name { get; set; } = "";
    public string Birthplace { get; set; } = "";

    [MarkoutPropertyName("Born")]
    public int BirthYear { get; set; }

    public string Citizenship { get; set; } = "";
}

[MarkoutSerializable]
public class ActorDetailRow
{
    public string Name { get; set; } = "";
    public string Birthplace { get; set; } = "";

    [MarkoutPropertyName("Born")]
    public int BirthYear { get; set; }

    public string Citizenship { get; set; } = "";

    [MarkoutPropertyName("Known For")]
    public string KnownFor { get; set; } = "";
}

[MarkoutSerializable]
public class ShowRow
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Years { get; set; } = "";

    [MarkoutPropertyName("Location")]
    public string FilmingLocation { get; set; } = "";
}

[MarkoutSerializable]
public class CityRow
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";

    [MarkoutPropertyName("Shows")]
    public int ShowCount { get; set; }

    [MarkoutPropertyName("Most Recent")]
    public string MostRecent { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ActorSearchResult
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results")]
    public List<ActorDetailRow>? Results { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class LocationSearchResult
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results")]
    public List<ShowRow>? Results { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ShowDetailView
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    public string Type { get; set; } = "";
    public string Years { get; set; } = "";

    [MarkoutPropertyName("Location")]
    public string FilmingLocation { get; set; } = "";

    [MarkoutSection(Name = "Canadian Cast")]
    public List<ActorRow>? Cast { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class ActorFilmography
{
    public string Name { get; set; } = "";
    public string Birthplace { get; set; } = "";
    public int BirthYear { get; set; }
    public string Citizenship { get; set; } = "";

    [MarkoutSection(Name = "Filmography")]
    public List<ShowRow>? Filmography { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class FilmographyTreeView
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public List<TreeNode>? Filmography { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ShowsByLocationChart
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public IReadOnlyList<BarItem>? Bars { get; set; }
}

[MarkoutContext(typeof(CanConOverview))]
[MarkoutContext(typeof(ActorRow))]
[MarkoutContext(typeof(ActorDetailRow))]
[MarkoutContext(typeof(ShowRow))]
[MarkoutContext(typeof(CityRow))]
[MarkoutContext(typeof(ActorSearchResult))]
[MarkoutContext(typeof(LocationSearchResult))]
[MarkoutContext(typeof(ShowDetailView))]
[MarkoutContext(typeof(ActorFilmography))]
[MarkoutContext(typeof(FilmographyTreeView))]
[MarkoutContext(typeof(ShowsByLocationChart))]
public partial class CanConContext : MarkoutSerializerContext { }
