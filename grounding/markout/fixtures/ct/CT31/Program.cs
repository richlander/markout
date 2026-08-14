using Markout;

var report = new PackageReport
{
    Title = "Packages",
    Rows =
    {
        new() { Name = "Markout", InternalId = "pkg-001" },
        new() { Name = "Spectre.Console", InternalId = "pkg-002" },
    },
};

MarkoutSerializer.Serialize(report, Console.Out, PackageReportContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Title))]
public sealed class PackageReport
{
    public string Title { get; init; } = "";

    [MarkoutSection(Name = "Inventory")]
    public List<PackageRow> Rows { get; init; } = [];
}

public sealed class PackageRow
{
    [MarkoutIgnore]
    public string Name { get; init; } = "";

    [MarkoutIgnore]
    public string InternalId { get; init; } = "";
}

[MarkoutContext(typeof(PackageReport))]
public partial class PackageReportContext : MarkoutSerializerContext;
