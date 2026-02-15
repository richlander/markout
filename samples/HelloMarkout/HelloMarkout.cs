using Markout;

var city = new CityView
{
    Name = "Vancouver",
    Country = "Canada",
    Population = 2_632_000,
    Temperature = 6.2,
    Latitude = 49.2827,
    Longitude = -123.1207,
    Altitude = 0
};

MarkoutSerializer.Serialize(city, Console.Out, CityContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class CityView
{
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    [MarkoutDisplayFormat("{0:N0}")]
    public int Population { get; set; }

    [MarkoutSection(Name = "Geography")]
    public double Latitude { get; set; }

    [MarkoutSection(Name = "Geography")]
    public double Longitude { get; set; }

    [MarkoutSection(Name = "Geography")]
    [MarkoutPropertyName("Altitude (m)")]
    public int Altitude { get; set; }

    [MarkoutSection(Name = "Geography")]
    [MarkoutDisplayFormat("{0:0.0} °C")]
    public double Temperature { get; set; }
}

[MarkoutContext(typeof(CityView))]
public partial class CityContext : MarkoutSerializerContext { }
