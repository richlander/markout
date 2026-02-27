using Markout;

var artist = new Artist(
    Name: "Sarah McLachlan",
    Genre: "Pop / Adult Contemporary",
    Origin: "Halifax, Nova Scotia",
    DebutYear: 1988,
    BestKnownFor: "Angel, Building a Mystery, Adia");

MarkoutSerializer.Serialize(artist, Console.Out, ArtistContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Artist.Name))]
public record Artist(
    string Name,
    string Genre,
    string Origin,
    int DebutYear,
    string BestKnownFor);

[MarkoutContext(typeof(Artist))]
public partial class ArtistContext : MarkoutSerializerContext { }
