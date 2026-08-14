using Markout;

var report = new UnlockReport
{
    Title = "Unlocks",
    Rows =
    {
        new MultiSourceRow(
            "grounded-only unlocks",
            new Source("mini", 5),
            new Source("mid", 1),
            new Source("frontier", 0))
        {
            Goal = Goal.Higher,
        },
    },
};

// TODO: Render Markdown by default and TSV for the "tsv" argument through the referenced
// serializer. Declaratively emphasize scalar source values at least 2 in rich output. Markdown
// should bold only qualifying values; TSV must remain unstyled. Do not add Markdown markers to
// labels or source values.

public sealed class UnlockReport
{
    public string Title { get; init; } = "";
    public List<MultiSourceRow> Rows { get; init; } = [];
}
