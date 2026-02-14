// CanCon. It's the law!

using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi;
using Microsoft.Extensions.Terminal;

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

var nIndex = argList.IndexOf("-n");
if (nIndex >= 0 && nIndex + 1 < argList.Count && int.TryParse(argList[nIndex + 1], out var n))
{
    maxItems = n;
    argList.RemoveRange(nIndex, 2);
}

var query = argList.Count > 0 ? string.Join(" ", argList).ToLowerInvariant() : "";

if (query is "-h" or "--help" or "help")
{
    Console.WriteLine("""
        Canadian Content Database - CanCon. It's the law!

        Usage: dotnet run -- [--format markdown|ansi|plain] [-n count] [query]

        Formats:
          markdown      Markdown output (default)
          ansi          ANSI terminal output with colors
          plain         Plain text output

        Options:
          -n count      Limit table rows to count (e.g. -n 5)

        Queries:
          (no args)     Show all actors and shows
          ryan          Actors named Ryan (Gosling, Reynolds)
          rachel        Actors named Rachel (McAdams, Skarsten)
          gosling       Ryan Gosling's filmography
          reynolds      Ryan Reynolds' filmography
          expanse       The Expanse with Canadian cast
          schitt        Schitt's Creek with Canadian cast
          toronto       Shows filmed in Toronto
          vancouver     Shows filmed in Vancouver
          tree          Filmography trees (shows grouped by filming location)

        Examples:
          dotnet run
          dotnet run -- ryan
          dotnet run -- --format ansi toronto
          dotnet run -- --format plain tree
        """);
    return;
}

// Create the writer for the selected format
var options = new MarkoutWriterOptions { MaxItems = maxItems };
MarkoutWriter writer = format switch
{
    "ansi" => new AnsiWriter(new AnsiTerminal(new SystemConsole()), options),
    "plain" => new MarkoutWriter(Console.Out, options),
    _ => new MarkdownWriter(Console.Out, options),
};

if (string.IsNullOrEmpty(query))
{
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
            .GroupBy(s => s.Location.Replace("Filmed in ", ""))
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
    writer.WriteHeading(1, "Canadian Content — Filmography Trees");

    foreach (var actor in actors)
    {
        var actorShows = shows
            .Where(s => s.CanadianActors.Contains(actor.Name))
            .ToList();

        if (actorShows.Count == 0) continue;

        // Group shows by filming location
        var cityNodes = actorShows
            .GroupBy(s => s.Location.Replace("Filmed in ", ""))
            .OrderBy(g => g.Key)
            .Select(g => new TreeNode(
                g.Key,
                g.Select(s => $"{s.Title} ({s.Years})")))
            .ToList();

        writer.WriteTree([new TreeNode(actor.Name, cityNodes)]);
        writer.WriteBlankLine();
    }
}
else
{
    Console.Error.WriteLine($"Unknown query: {query}");
    Console.Error.WriteLine("Try: ryan, rachel, gosling, reynolds, expanse, schitt, toronto, vancouver, tree");
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

    [MarkoutPropertyName("Filmed In")]
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

    [MarkoutPropertyName("Filmed In")]
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

[MarkoutContext(typeof(CanConOverview))]
[MarkoutContext(typeof(ActorRow))]
[MarkoutContext(typeof(ActorDetailRow))]
[MarkoutContext(typeof(ShowRow))]
[MarkoutContext(typeof(CityRow))]
[MarkoutContext(typeof(ActorSearchResult))]
[MarkoutContext(typeof(LocationSearchResult))]
[MarkoutContext(typeof(ShowDetailView))]
[MarkoutContext(typeof(ActorFilmography))]
public partial class CanConContext : MarkoutSerializerContext { }
