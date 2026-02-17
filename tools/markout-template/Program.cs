using Markout;
using Markout.Templates;
using MarkdownTable.Formatting;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return;
}

var templatePath = args[0];

if (!File.Exists(templatePath))
{
    Console.Error.WriteLine($"Template file not found: {templatePath}");
    return;
}

var template = MarkoutTemplate.Load(templatePath);
template.TableOptions = new TableFormatterOptions();
template.SkipUnboundPlaceholders = true;

// Bind key=value pairs from remaining args
for (int i = 1; i < args.Length; i++)
{
    var arg = args[i];
    var eqIndex = arg.IndexOf('=');

    if (eqIndex > 0)
    {
        var key = arg[..eqIndex];
        var value = arg[(eqIndex + 1)..];
        template.Bind(key, value);
    }
}

// Bind stdin as a data source if piped
if (!Console.IsInputRedirected)
{
    var options = new MarkoutWriterOptions { PrettyTables = true };
    Console.Write(template.Render(options));
}
else
{
    // Read stdin as key=value lines (blank-line separated from template bindings)
    using var reader = Console.In;
    string? line;
    while ((line = reader.ReadLine()) is not null)
    {
        var eqIndex = line.IndexOf('=');
        if (eqIndex > 0)
        {
            var key = line[..eqIndex].Trim();
            var value = line[(eqIndex + 1)..].Trim();
            template.Bind(key, value);
        }
    }

    var options = new MarkoutWriterOptions { PrettyTables = true };
    Console.Write(template.Render(options));
}

static void PrintUsage()
{
    Console.WriteLine("""
        markout-template — render a Markout template with bound values

        Usage:
          markout-template <template.md> [key=value ...]

        Arguments:
          template.md    Path to the template file
          key=value      Bind a string value to a placeholder

        Pipe key=value lines on stdin for additional bindings.

        Examples:
          markout-template report.md date="February 2026" title="Monthly Report"
          echo "date=February 2026" | markout-template report.md
        """);
}
