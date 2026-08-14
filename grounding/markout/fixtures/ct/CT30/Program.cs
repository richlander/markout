using Markout;

var report = new NumberFormatReport
{
    Title = "Build Delta",
    Methods = new Change<int>(165, 1168),
};

// TODO: Render the report through the referenced serializer. Apply a numeric format to the
// Change<int> cell so both operands and its absolute delta use thousands separators. Keep the
// value as Change<int>; do not preformat the numbers or hand-write Markdown.

public sealed class NumberFormatReport
{
    public string Title { get; init; } = "";
    public Change<int> Methods { get; init; }
}
