// CanCon. It's the law!

using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;
using Markout.Ansi;
using Markout.Ansi.Spectre;
using Markout.Formatting;
using Microsoft.Extensions.Terminal;
using Spectre.Console;
using TreeNode = Markout.TreeNode;

var formatOption = new Option<string>("--format", "-f") { DefaultValueFactory = _ => "markdown", Description = "Output format" };
formatOption.AcceptOnlyFromAmong("markdown", "ansi", "spectre", "plain", "oneline", "diagram");

var maxItemsOption = new Option<int?>("-n") { Description = "Limit table rows" };
var prettyOption = new Option<bool>("--pretty") { Description = "Pad table columns for aligned output" };
var actorsOption = new Option<bool>("--actors") { Description = "Filter summary to actors table" };
var showsOption = new Option<bool>("--shows") { Description = "Filter summary to shows table" };
var citiesOption = new Option<bool>("--cities") { Description = "Filter summary to filming locations table" };
var queryArg = new Argument<string>("query") { DefaultValueFactory = _ => "help", Description = "Query to run" };

var rootCommand = new RootCommand("Canadian Content Database — CanCon. It's the law!")
{
    formatOption, maxItemsOption, prettyOption,
    actorsOption, showsOption, citiesOption, queryArg
};

rootCommand.SetAction(Run);
var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();

async Task Run(ParseResult parseResult, CancellationToken ct)
{
    var format = parseResult.GetValue(formatOption)!;
    var maxItems = parseResult.GetValue(maxItemsOption);
    bool prettyTables = parseResult.GetValue(prettyOption);
    var query = parseResult.GetValue(queryArg)!.ToLowerInvariant();

    HashSet<string>? includeSections = null;
    if (parseResult.GetValue(actorsOption)) (includeSections ??= []).Add("Actors");
    if (parseResult.GetValue(showsOption)) (includeSections ??= []).Add("Shows");
    if (parseResult.GetValue(citiesOption)) (includeSections ??= []).Add("Filming Locations");

    // Load data from JSON files
    var basePath = AppContext.BaseDirectory;
    var actorsJson = await File.ReadAllTextAsync(Path.Combine(basePath, "actors.json"), ct);
    var showsJson = await File.ReadAllTextAsync(Path.Combine(basePath, "shows.json"), ct);
    var citiesJson = await File.ReadAllTextAsync(Path.Combine(basePath, "cities.json"), ct);

    var actors = JsonSerializer.Deserialize(actorsJson, CanConJsonContext.Default.ListActor)!;
    var shows = JsonSerializer.Deserialize(showsJson, CanConJsonContext.Default.ListShow)!;
    var cityCountry = JsonSerializer.Deserialize(citiesJson, CanConJsonContext.Default.ListCity)!
        .ToDictionary(c => c.CityName, c => c.Country);
    var options = new MarkoutWriterOptions { MaxItems = maxItems, IncludeSections = includeSections, PrettyTables = prettyTables };

    (IMarkoutFormatter formatter, StringWriter output) CreateWriter()
    {
        var sw = new StringWriter();
        var terminal = new AnsiTerminal(new SystemConsole());
        IMarkoutFormatter f = format switch
        {
            "ansi" => new AnsiWriter(terminal),
            "spectre" => new SpectreWriter(AnsiConsole.Console),
            "oneline" => new OneLineFormatter(),
            "diagram" => new DiagramWriter(),
            _ => new MarkdownFormatter(),
        };
        return (f, sw);
    }

    StringWriter? result = query switch
    {
        var q when q.Contains("summary") => Summary(),
        var q when q.Contains("ryan") => ActorsByName("Ryan"),
        var q when q.Contains("rachel") => ActorsByName("Rachel"),
        var q when q.Contains("expanse") => ShowDetail("Expanse"),
        var q when q.Contains("schitt") => ShowDetail("Schitt"),
        var q when q.Contains("toronto") => ShowsByLocation("Toronto"),
        var q when q.Contains("vancouver") => ShowsByLocation("Vancouver"),
        var q when q.Contains("gosling") => Filmography("Gosling"),
        var q when q.Contains("reynolds") => Filmography("Reynolds"),
        var q when q.Contains("tree") => FilmographyTrees(),
        var q when q.Contains("vbars") => VerticalBars(),
        var q when q.Contains("bars") => HorizontalBars(),
        var q when q.Contains("genre") => GenreBreakdown(),
        var q when q.Contains("report") => Report(),
        _ => null
    };

    if (result is null)
    {
        Console.Error.WriteLine($"Unknown query: {query}\nTry: summary, ryan, rachel, gosling, reynolds, expanse, schitt, toronto, vancouver, tree, bars, vbars, genre, report");
        return;
    }

    Console.Out.Write(result.ToString());

    // ── Query implementations ──

    StringWriter Summary()
    {
        var (fmt, output) = CreateWriter();
        if (format == "oneline" && includeSections == null)
        {
            Console.Error.WriteLine("oneline format requires a section flag with summary: --actors, --shows, or --cities");
            return output;
        }
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
        MarkoutSerializer.Serialize(overview, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter ActorsByName(string nameFilter)
    {
        var (fmt, output) = CreateWriter();
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
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter ShowDetail(string titleFragment)
    {
        var (fmt, output) = CreateWriter();
        var show = shows.First(s => s.Title.Contains(titleFragment, StringComparison.OrdinalIgnoreCase));
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
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter ShowsByLocation(string city)
    {
        var (fmt, output) = CreateWriter();
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
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter Filmography(string lastName)
    {
        var (fmt, output) = CreateWriter();
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
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter FilmographyTrees()
    {
        var (fmt, output) = CreateWriter();
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
                    return new TreeNode(actor.Name,
                        actorShows.GroupBy(s => s.Location).OrderBy(g => g.Key)
                            .Select(g => new TreeNode(g.Key, g.Select(s => $"{s.Title} ({s.Years})"))));
                })
                .Where(n => n is not null)
                .ToList()!
        };
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter VerticalBars()
    {
        var (fmt, output) = CreateWriter();
        var orch = MarkoutWriter.Create(output, fmt, options);
        var bars = shows
            .GroupBy(s => s.Location)
            .OrderByDescending(g => g.Count())
            .Select(g => new Metric(g.Key, g.Count()))
            .ToList();
        orch.WriteHeading(1, "Shows per Filming Location");
        orch.WriteVerticalMetrics(bars);
        return output;
    }

    StringWriter HorizontalBars()
    {
        var (fmt, output) = CreateWriter();
        var view = new ShowsByLocationChart
        {
            Title = "Shows per Filming Location",
            Bars = shows
                .GroupBy(s => s.Location)
                .OrderByDescending(g => g.Count())
                .Select(g => new Metric(g.Key, g.Count()))
                .ToList()
        };
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter GenreBreakdown()
    {
        var (fmt, output) = CreateWriter();
        var view = new GenreBreakdownView
        {
            Title = "Canadian Content — Genre Breakdown",
            Breakdown = [new Breakdown("All Shows", shows
                .GroupBy(s => s.Type)
                .OrderByDescending(g => g.Count())
                .Select(g => new Segment(g.Key, g.Count()))
                .ToArray())]
        };
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }

    StringWriter Report()
    {
        var (fmt, output) = CreateWriter();
        var topActors = actors.Take(3).ToList();
        var view = new CanConReportView
        {
            Title = "Canadian Content Report",
            Description = "The CRTC's Canadian content regulations require broadcasters to air a minimum percentage of Canadian programming. This report covers top actors, filming locations, and genre distribution.",
            Mandate = new Callout(CalloutSeverity.Important, "Canadian content rules require 60% Canadian programming on conventional TV and 35% on radio."),
            TopActors = topActors.Select(a => new ActorRow
            {
                Name = a.Name, Birthplace = a.Birthplace, BirthYear = a.BirthYear,
                Citizenship = string.Join(", ", a.Citizenship)
            }).ToList(),
            ActorBios = topActors.Select(a => new Description(
                a.Name,
                $"Born {a.BirthYear} in {a.Birthplace}. Known for {string.Join(", ", a.Shows.Take(2))}."
            )).ToList(),
            ShowsPerCity = shows.GroupBy(s => s.Location).OrderByDescending(g => g.Count())
                .Select(g => new Metric(g.Key, g.Count()))
                .ToList(),
            GenreMix = [new Breakdown("All Shows", shows
                .GroupBy(s => s.Type)
                .OrderByDescending(g => g.Count())
                .Select(g => new Segment(g.Key, g.Count()))
                .ToArray())],
            FilmographyTree = topActors.Select(actor =>
            {
                var actorShows = shows.Where(s => s.CanadianActors.Contains(actor.Name)).ToList();
                return new TreeNode(actor.Name,
                    actorShows.GroupBy(s => s.Location).Select(g =>
                        new TreeNode(g.Key, g.Select(s => s.Title))));
            }).ToList(),
            Quote = "The world needs more Canada.\n— Bono, 2003"
        };
        MarkoutSerializer.Serialize(view, output, fmt, CanConContext.Default, options);
        return output;
    }
}

// --- JSON Models (camelCase naming via options) ---

public class Actor
{
    public string Name { get; set; } = "";
    public string Birthplace { get; set; } = "";
    public int BirthYear { get; set; }
    public List<string> Citizenship { get; set; } = new();
    public List<string> Shows { get; set; } = new();
}

public class Show
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Years { get; set; } = "";
    public string Location { get; set; } = "";
    public List<string> CanadianActors { get; set; } = new();
}

public class City
{
    [JsonPropertyName("city")]
    public string CityName { get; set; } = "";
    public string Country { get; set; } = "";
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<Actor>))]
[JsonSerializable(typeof(List<Show>))]
[JsonSerializable(typeof(List<City>))]
internal partial class CanConJsonContext : JsonSerializerContext { }

// --- Markout View Models ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CanConOverview
{
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
    [MarkoutValueMap("Movie=🎬", "TV Series=📺", "TV Miniseries=📺")]
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
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results")]
    public List<ActorDetailRow>? Results { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class LocationSearchResult
{
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results")]
    public List<ShowRow>? Results { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ShowDetailView
{
    public string Title { get; set; } = "";

    [MarkoutValueMap("Movie=🎬", "TV Series=📺", "TV Miniseries=📺")]
    public string Type { get; set; } = "";
    public string Years { get; set; } = "";

    [MarkoutPropertyName("Location")]
    public string FilmingLocation { get; set; } = "";

    [MarkoutSection(Name = "Cast")]
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
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public List<TreeNode>? Filmography { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ShowsByLocationChart
{
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public IReadOnlyList<Metric>? Bars { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GenreBreakdownView
{
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public IReadOnlyList<Breakdown>? Breakdown { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title), DescriptionProperty = nameof(Description))]
public class CanConReportView
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    [MarkoutIgnoreInTable]
    [MarkoutSkipDefault]
    public Callout Mandate { get; set; }

    [MarkoutSection(Name = "Top Actors")]
    public List<ActorRow>? TopActors { get; set; }

    [MarkoutSection(Name = "Actor Profiles")]
    public List<Description>? ActorBios { get; set; }

    [MarkoutSection(Name = "Shows per City")]
    public IReadOnlyList<Metric>? ShowsPerCity { get; set; }

    [MarkoutSection(Name = "Genre Mix")]
    public IReadOnlyList<Breakdown>? GenreMix { get; set; }

    [MarkoutSection(Name = "Filmography")]
    public List<TreeNode>? FilmographyTree { get; set; }

    [MarkoutIgnore]
    public string? Quote { get; set; }
}

public partial class CanConReportViewMarkoutTypeInfo
{
    partial void OnSerialized(MarkoutWriter writer, CanConReportView value)
    {
        if (value.Quote != null)
        {
            writer.WriteQuotation(value.Quote);
        }
    }
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
[MarkoutContext(typeof(GenreBreakdownView))]
[MarkoutContext(typeof(CanConReportView))]
public partial class CanConContext : MarkoutSerializerContext { }
