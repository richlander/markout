using Markout;

var report = new OrganizationReport
{
    Title = "Organization",
    Rows =
    {
        new() { Name = "Platform", Count = 12 },
        new() { Name = "Runtime", Count = 5, IsChild = true },
        new() { Name = "Tools", Count = 3, IsChild = true },
        new() { Name = "Product", Count = 8 },
    },
};

// TODO: Render the report through the referenced serializer. Mark IsChild as the semantic
// child-row flag so true rows nest under the preceding parent without becoming a table column.
// Do not add child glyphs to Name values or hand-write Markdown.

public sealed class OrganizationReport
{
    public string Title { get; init; } = "";
    public List<OrganizationRow> Rows { get; init; } = [];
}

public sealed class OrganizationRow
{
    public string Name { get; init; } = "";
    public int Count { get; init; }
    public bool IsChild { get; init; }
}
