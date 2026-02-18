using MarkdownTable.Query;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    ShowHelp();
    return;
}

// Parse arguments: mq '<query>' [file]
string query;
string? inputFile = null;

if (args[0] == "-t" && args.Length >= 3)
{
    // mq -t <section> '<query>' [file]
    // Reserved for future section targeting via flag
    Console.Error.WriteLine("The -t flag is not yet implemented. Use .SectionName in the query instead.");
    Environment.Exit(1);
    return;
}

query = args[0];
if (args.Length > 1)
    inputFile = args[1];

// Read input
string input;
if (!string.IsNullOrEmpty(inputFile))
{
    if (!File.Exists(inputFile))
    {
        Console.Error.WriteLine($"mq: file not found: {inputFile}");
        Environment.Exit(1);
        return;
    }
    input = File.ReadAllText(inputFile);
}
else if (Console.IsInputRedirected)
{
    input = Console.In.ReadToEnd();
}
else
{
    Console.Error.WriteLine("mq: No input file specified and no data on stdin.");
    Console.Error.WriteLine("Usage: mq '<query>' [file]  or  command | mq '<query>'");
    Console.Error.WriteLine("Use 'mq --help' for more information.");
    Environment.Exit(1);
    return;
}

// Execute query
try
{
    var result = QueryEngine.Execute(input, query);
    Console.Write(QueryEngine.FormatResult(result));
}
catch (QueryParseException ex)
{
    Console.Error.WriteLine($"mq: parse error: {ex.Message}");
    if (query.Length > 0)
    {
        Console.Error.WriteLine($"  {query}");
        Console.Error.WriteLine($"  {new string(' ', Math.Max(0, ex.Position))}^");
    }
    Environment.Exit(1);
}
catch (QueryExecutionException ex)
{
    Console.Error.WriteLine($"mq: {ex.Message}");
    Environment.Exit(1);
}

static void ShowHelp()
{
    Console.WriteLine("mq - Markdown Query");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("    mq '<query>' [file]");
    Console.WriteLine("    command | mq '<query>'");
    Console.WriteLine();
    Console.WriteLine("QUERY SYNTAX:");
    Console.WriteLine("    .SectionName                 Select table from a named section");
    Console.WriteLine("    select .Col1, .Col2          Project columns");
    Console.WriteLine("    where .Col == \"value\"        Filter rows");
    Console.WriteLine("    orderby .Col [asc|desc]      Sort rows");
    Console.WriteLine("    take N / skip N              Limit/offset rows");
    Console.WriteLine("    first / last                 First or last row");
    Console.WriteLine("    count                        Count rows");
    Console.WriteLine("    distinct                     Remove duplicate rows");
    Console.WriteLine("    .[0]                         Row by index");
    Console.WriteLine("    .[0:5]                       Row slice");
    Console.WriteLine("    .[].Col                      Extract column values");
    Console.WriteLine("    .[0].Col                     Extract single cell value");
    Console.WriteLine();
    Console.WriteLine("Operations are chained with | (pipe):");
    Console.WriteLine("    mq 'where .Status == \"Active\" | select .Name, .CPU | orderby .CPU desc'");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("    mq '.[0:5]' table.md                    # First 5 rows");
    Console.WriteLine("    mq '.Methods | where .Name == \"Get\"' api.md");
    Console.WriteLine("    mq 'orderby .Score desc | take 10' grades.md");
    Console.WriteLine("    cat report.md | mq '.\"All Releases\" | where .Type == \"LTS\"'");
}
