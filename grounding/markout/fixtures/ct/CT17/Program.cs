// An API scanner produced these package signals. Plain data — it carries no output formatting.

var packages = new List<PackageInfo>
{
    new() { Id = "Newtonsoft.Json",
            Apis = new() { new() { Member = "JsonConvert.SerializeObject", Kind = "Method" } } },
    new() { Id = "System.Runtime.CompilerServices.Unsafe",
            Types = new() { new() { TypeName = "Unsafe", Category = "Static" } } },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class PackageInfo
{
    public string Id { get; init; } = "";
    public List<Api> Apis { get; init; } = new();
    public List<TypeRow> Types { get; init; } = new();
}

public sealed class Api { public string Member { get; init; } = ""; public string Kind { get; init; } = ""; }

public sealed class TypeRow { public string TypeName { get; init; } = ""; public string Category { get; init; } = ""; }
