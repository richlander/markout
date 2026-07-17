// A package inspection tool produced this report. Plain data — it carries no output formatting.

var pkg = new PackageInfo
{
    Id = "Newtonsoft.Json",
    Downloads = 5_100_000_000,
    Signed = true,
    Deprecations = new() { "JsonConvert legacy Formatting overload" },
    Dependencies = new()
    {
        new() { Name = "System.Text.Encodings.Web", Version = "8.0.0", Optional = false },
        new() { Name = "Microsoft.Bcl.AsyncInterfaces", Version = "8.0.0", Optional = true },
    },
    Apis = new() { new() { Member = "JsonConvert.SerializeObject", Kind = "Method" } },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PackageInfo
{
    public string Id { get; init; } = "";
    public long Downloads { get; init; }
    public bool Signed { get; init; }
    public List<string> Deprecations { get; init; } = new();
    public List<Dep> Dependencies { get; init; } = new();
    public List<Api> Apis { get; init; } = new();
    public List<TypeRow> Types { get; init; } = new();
}

public sealed class Dep
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public bool Optional { get; init; }
}

public sealed class Api { public string Member { get; init; } = ""; public string Kind { get; init; } = ""; }

public sealed class TypeRow { public string TypeName { get; init; } = ""; public string Category { get; init; } = ""; }
