// A compiler run produced these errors and a dependency graph. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Diagnostics";

var errors = new List<CompileError>
{
    new("CS0103", "name does not exist"),
    new("CS1002", "; expected"),
};

// Dependency graph: App -> { Markout, Serilog -> { Serilog.Sinks.Console } }
var dependencies = new DependencyNode("App", new List<DependencyNode>
{
    new("Markout", new List<DependencyNode>()),
    new("Serilog", new List<DependencyNode>
    {
        new("Serilog.Sinks.Console", new List<DependencyNode>()),
    }),
});

// TODO: print a Markdown report with an "Errors" table and a "Dependencies" tree (see task prompt).

public record CompileError(string Code, string Message);
public record DependencyNode(string Name, List<DependencyNode> Children);
