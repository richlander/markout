// A dependency restore produced this package list. Render it as a Markdown table (see task).
// These are plain data objects — they carry no output formatting.

var packages = new List<PackageRow>
{
    new("Serilog", "3.1.1"),
    new("Polly", "8.2.0"),
    new("Newtonsoft.Json", "13.0.3"),
};

// TODO: print a GitHub-flavored Markdown table of these packages to the console (see task prompt).

public record PackageRow(string Name, string Version);
