// Demonstrates [MarkoutSkipNull] — optional fields are omitted when null.
// Type-level [MarkoutSkipNull] applies to all nullable properties.

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
[MarkoutSkipNull]
public class PackageView
{
    public string Name { get; set; } = "";
    public string? License { get; set; }
    public string? Repository { get; set; }
    public int Downloads { get; set; }
    public int? Stars { get; set; }
}

[MarkoutContext(typeof(PackageView))]
public partial class PackageContext : MarkoutSerializerContext { }
