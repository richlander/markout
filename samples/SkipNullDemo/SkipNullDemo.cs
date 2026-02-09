#!/usr/bin/env dotnet run
#:package Markout@0.5.0

// Demonstrates [MarkoutSkipNull] — optional fields are omitted when null.

using Markout;

// Only Name and License are set — Repository and Stars are omitted from output
var pkg = new PackageView
{
    Name = "Markout",
    License = "MIT",
    Repository = null,
    Downloads = 1200,
    Stars = null
};

MarkoutSerializer.Serialize(pkg, Console.Out, PackageContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class PackageView
{
    public string Name { get; set; } = "";
    [MarkoutSkipNull]
    public string? License { get; set; }
    [MarkoutSkipNull]
    public string? Repository { get; set; }
    public int Downloads { get; set; }
    [MarkoutSkipNull]
    public int? Stars { get; set; }
}

[MarkoutContext(typeof(PackageView))]
public partial class PackageContext : MarkoutSerializerContext { }
