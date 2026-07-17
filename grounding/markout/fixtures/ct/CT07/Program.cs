// A package audit produced these records. Plain data — it carries no output formatting.

var packages = new List<PackageInfo>
{
    new() { Id = "Newtonsoft.Json", Version = "13.0.3",
            Deprecations = new() { "JsonConvert legacy Formatting overload" } },
    new() { Id = "System.Text.Json", Version = "9.0.0", Deprecations = new() },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PackageInfo
{
    public string Id { get; init; } = "";
    public string Version { get; init; } = "";
    public List<string> Deprecations { get; init; } = new();
}
