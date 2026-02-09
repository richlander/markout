#!/usr/bin/env dotnet run
#:package Markout@0.4.0

using Markout;

var city = new CityView
{
    Name = "Vancouver",
    Country = "Canada",
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
    public double Temperature { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
}

[MarkoutContext(typeof(CityView))]
public partial class CityContext : MarkoutSerializerContext { }
