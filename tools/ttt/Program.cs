using MarkdownTable.Formatting;
using ttt;

var options = DynamicArgumentParser.ParseArguments(args);

if (options.ShowHelp)
{
    ShowHelp();
    return;
}

if (options.ShowModes)
{
    ShowModes();
    return;
}

// Read input
string input;
if (!string.IsNullOrEmpty(options.InputFile))
{
    if (!File.Exists(options.InputFile))
    {
        Console.Error.WriteLine($"File not found: {options.InputFile}");
        Environment.Exit(1);
        return;
    }
    input = File.ReadAllText(options.InputFile);
}
else if (Console.IsInputRedirected)
{
    input = Console.In.ReadToEnd();
}
else
{
    Console.Error.WriteLine("ttt: No input file specified and no data on stdin.");
    Console.Error.WriteLine("Usage: ttt [OPTIONS] [FILE]  or  command | ttt [OPTIONS]");
    Console.Error.WriteLine("Use 'ttt --help' for more information.");
    Environment.Exit(1);
    return;
}

// Validate mode
if (options.Mode is null)
{
    Console.Error.WriteLine($"Error: Unknown mode '{options.ModeName}'");
    Console.Error.WriteLine();
    ShowModes();
    Environment.Exit(1);
    return;
}

// Reformat and write
var result = DocumentFormatter.ReformatDocument(input, options.Mode.Options);
Console.Write(result);

static void ShowHelp()
{
    var modes = FormatterRegistry.GetModes().Select(m => m.Name).ToArray();
    var modesList = string.Join(", ", modes);

    Console.WriteLine("ttt - Table To Table");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("    ttt [OPTIONS] [FILE]");
    Console.WriteLine();
    Console.WriteLine("OPTIONS:");
    Console.WriteLine($"    -r, --renderer <MODE>   Formatter mode: {modesList} [default: smooth]");
    Console.WriteLine("    --show-modes            Show detailed information about modes");
    Console.WriteLine("    -h, --help              Show this help message");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("    ttt document.md                          # Format with smooth mode");
    Console.WriteLine("    ttt -r compact document.md               # Format with compact mode");
    Console.WriteLine("    ttt -r compact --percentile 0.3 file.md  # Compact with custom percentile");
    Console.WriteLine("    echo '| a | b |' | ttt                   # Format from stdin");
}

static void ShowModes()
{
    Console.WriteLine("Available modes:");
    Console.WriteLine();

    foreach (var (name, description) in FormatterRegistry.GetModes())
    {
        Console.WriteLine($"  {name,-10}  {description}");
    }
}
