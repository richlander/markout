// These domain types come from a dependency and cannot be annotated. DependencyGroup contains a
// nested package collection, so registering it as a table row normally produces MARKOUT001.

var report = new DependencyReport
{
    Title = "Dependencies",
    Groups =
    {
        new()
        {
            Framework = "net10.0",
            Packages =
            {
                new("Markout", "0.35.2"),
                new("Spectre.Console", "0.49.1"),
            },
        },
    },
};

// TODO: Render the report through the referenced serializer. Configure the generated serializer
// context to suppress expected table warnings globally; do not modify DependencyGroup with a
// per-property ignore attribute or a pragma.

public sealed class DependencyReport
{
    public string Title { get; init; } = "";
    public List<DependencyGroup> Groups { get; init; } = [];
}

public sealed class DependencyGroup
{
    public string Framework { get; init; } = "";
    public List<PackageReference> Packages { get; init; } = [];
}

public sealed record PackageReference(string Id, string Version);
