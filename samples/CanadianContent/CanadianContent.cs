#!/usr/bin/env dotnet run
#:package Markout@0.2.3
// CanCon. It's the law!

using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;

// Load data from JSON files (relative to the source file location)
var basePath = Path.GetDirectoryName(Path.GetFullPath("CanadianContent.cs")) ?? ".";
var actorsJson = await File.ReadAllTextAsync(Path.Combine(basePath, "actors.json"));
var showsJson = await File.ReadAllTextAsync(Path.Combine(basePath, "shows.json"));

var actors = JsonSerializer.Deserialize(actorsJson, CanConJsonContext.Default.ListActor)!;
var shows = JsonSerializer.Deserialize(showsJson, CanConJsonContext.Default.ListShow)!;

// Parse command-line query
var query = args.Length > 0 ? string.Join(" ", args).ToLowerInvariant() : "";

if (query is "-h" or "--help" or "help")
{
    Console.WriteLine("""
        Canadian Content Database - CanCon. It's the law!

        Usage: dotnet run CanadianContent.cs [query]

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

        Examples:
          dotnet run CanadianContent.cs
          dotnet run CanadianContent.cs ryan
          dotnet run CanadianContent.cs toronto
        """);
    return;
}

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
        }).ToList()
    };
    MarkoutSerializer.Serialize(overview, Console.Out, CanConContext.Default);
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
    MarkoutSerializer.Serialize(view, Console.Out, CanConContext.Default);
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
    MarkoutSerializer.Serialize(view, Console.Out, CanConContext.Default);
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
    MarkoutSerializer.Serialize(view, Console.Out, CanConContext.Default);
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
    MarkoutSerializer.Serialize(view, Console.Out, CanConContext.Default);
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
    MarkoutSerializer.Serialize(view, Console.Out, CanConContext.Default);
}
else
{
    Console.WriteLine($"Unknown query: {query}");
    Console.WriteLine("Try: ryan, rachel, gosling, reynolds, expanse, schitt, toronto, vancouver");
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
[MarkoutContext(typeof(ActorSearchResult))]
[MarkoutContext(typeof(LocationSearchResult))]
[MarkoutContext(typeof(ShowDetailView))]
[MarkoutContext(typeof(ActorFilmography))]
public partial class CanConContext : MarkoutSerializerContext { }
