// A dependency graph tool produced this tree. Plain data — it carries no output formatting.

using Markout;

var report = new DependencyTree
{
    Project = "MyApp",
    Root = new TreeNode("MyApp",
    [
        new TreeNode("Serilog", [ new TreeNode("Serilog.Sinks.Console") ]) { Badge = "✓" },
        new TreeNode("Polly") { Badge = "✓" },
    ]),
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class DependencyTree
{
    public string Project { get; init; } = "";
    public TreeNode Root { get; init; } = null!;
}
