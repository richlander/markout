// A dependency restore produced this package list. Emit it as TSV for piping (see task).
// These are plain data objects — they carry no output formatting.

var packages = new List<PackageRow>
{
    new("Serilog", "3.1.1"),
    new("Polly", "8.2.0"),
    new("Newtonsoft.Json", "13.0.3"),
};

// TODO: emit these packages as tab-separated values (TSV) to the console (see task prompt).

public record PackageRow(string Name, string Version);
