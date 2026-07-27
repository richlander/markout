// A package index supplied this record. Plain data — it carries no output formatting.

var pkg = new PackageInfo
{
    Id = "Newtonsoft.Json",
    Downloads = 5_100_000_000,
    Published = new DateTime(2023, 3, 8),
    Signed = true,
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PackageInfo
{
    public string Id { get; init; } = "";
    public long Downloads { get; init; }
    public DateTime Published { get; init; }
    public bool Signed { get; init; }
}
