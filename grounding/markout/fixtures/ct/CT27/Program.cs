// A package inspection produced this data. Keep PackageSizeBytes numeric in the model.

var report = new PackageStats
{
    Id = "Acme.Tools",
    PackageSizeBytes = 2_411_724,
};

// TODO: Render the report through the referenced serializer. Format PackageSizeBytes as a
// human-readable binary size ("2.3 MB") with a typed custom value formatter wired to the numeric
// property. Do not replace the long with a preformatted string or format it in the getter.

public sealed class PackageStats
{
    public string Id { get; init; } = "";
    public long PackageSizeBytes { get; init; }
}
