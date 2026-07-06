// A build produced this dependency graph. Render it as a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Dependency Report";

// Dependency graph: App -> { Markout, Serilog -> { Serilog.Sinks.Console } }
var graph = new DepNode("App", new List<DepNode>
{
    new("Markout", new List<DepNode>()),
    new("Serilog", new List<DepNode>
    {
        new("Serilog.Sinks.Console", new List<DepNode>()),
    }),
});

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title, then a
// "## Dependencies" section that renders the graph as a tree using the library's tree
// shape (do not hand-write the tree). See task prompt.

public record DepNode(string Name, List<DepNode> Children);
