using Markout;

var artist = new ArtistView(
    Name: "Sarah McLachlan",
    Genre: "Pop / Adult Contemporary",
    Origin: "Halifax, Nova Scotia",
    DebutYear: 1988,
    BestKnownFor: "Angel, Building a Mystery, Adia");

MarkoutSerializer.Serialize(artist, Console.Out, ArtistContext.Default);

[MarkoutSerializable(TitleProperty = nameof(ArtistView.Name))]
public record ArtistView(
    string Name,
    string Genre,
    string Origin,
    int DebutYear,
    string BestKnownFor);

[MarkoutContext(typeof(ArtistView))]
public partial class ArtistContext : MarkoutSerializerContext { }
