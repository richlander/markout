using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MarkdownTable.Formatting;
using MarkdownTable.Query;

// Find repo root by walking up from the binary location
string repoRoot = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(repoRoot, "Markout.sln")))
    repoRoot = Path.GetDirectoryName(repoRoot) ?? throw new InvalidOperationException("Could not find repo root");

string mdText = File.ReadAllText(Path.Combine(repoRoot, "tests", "mq-bench", "releases.md"));
string jsonText = File.ReadAllText(Path.Combine(repoRoot, "tests", "mq-bench", "releases.json"));
byte[] mdBytes = Encoding.UTF8.GetBytes(mdText);
string mdFilePath = Path.Combine(repoRoot, "tests", "mq-bench", "releases.md");

// Warmup + correctness
Console.WriteLine("=== Correctness ===");
Console.WriteLine();

var queries = new (string Label, string MqQuery, Func<string, string> JqEquiv)[]
{
    ("Count", "count",
        json => JsonDocument.Parse(json).RootElement.GetArrayLength().ToString()),

    ("Filter count (LTS)", "where .Type == \"LTS\" | count",
        json =>
        {
            int c = 0;
            foreach (var el in JsonDocument.Parse(json).RootElement.EnumerateArray())
                if (el.GetProperty("Type").GetString() == "LTS") c++;
            return c.ToString();
        }),

    ("First scalar", ".[0].Version",
        json => JsonDocument.Parse(json).RootElement[0].GetProperty("Version").GetString()!),

    ("Last scalar", ".[-1].Version",
        json =>
        {
            var arr = JsonDocument.Parse(json).RootElement;
            return arr[arr.GetArrayLength() - 1].GetProperty("Version").GetString()!;
        }),

    ("Select columns", "select .Version, .Type",
        json =>
        {
            var sb = new System.Text.StringBuilder();
            foreach (var el in JsonDocument.Parse(json).RootElement.EnumerateArray())
                sb.AppendLine($"{el.GetProperty("Version").GetString()}\t{el.GetProperty("Type").GetString()}");
            return sb.ToString().TrimEnd();
        }),

    ("Filter + project", "where .Type == \"LTS\" | .[].Version",
        json =>
        {
            var sb = new System.Text.StringBuilder();
            foreach (var el in JsonDocument.Parse(json).RootElement.EnumerateArray())
                if (el.GetProperty("Type").GetString() == "LTS")
                    sb.AppendLine(el.GetProperty("Version").GetString());
            return sb.ToString().TrimEnd();
        }),
};

foreach (var (label, mqQuery, jqEquiv) in queries)
{
    var mqResult = QueryEngine.FormatResult(QueryEngine.Execute(mdText, mqQuery)).Trim();
    var jqResult = jqEquiv(jsonText).Trim();
    // For table outputs, just compare row count or first line
    Console.WriteLine($"  {label,-20} mq={Truncate(mqResult)}  json={Truncate(jqResult)}");
}

Console.WriteLine();

// Benchmark
const int WarmupIterations = 1_000;
const int MeasureIterations = 100_000;

Console.WriteLine($"=== Performance ({MeasureIterations:N0} iterations, Release build) ===");
Console.WriteLine();

// Pre-parse for "library call" benchmarks
var parsedDoc = DocumentReader.Read(mdText);
var parsedJsonDoc = JsonDocument.Parse(jsonText);

var benchmarks = new (string Label, string Category, Action Action)[]
{
    // --- Count ---
    ("mq string", "Count",
        () => QueryEngine.Execute(mdText, "count")),
    ("mq MemoryStream", "Count",
        () => { using var ms = new MemoryStream(mdBytes); QueryEngine.ExecuteAsync(ms, "count").GetAwaiter().GetResult(); }),
    ("mq FileStream", "Count",
        () => { using var fs = File.OpenRead(mdFilePath); QueryEngine.ExecuteAsync(fs, "count").GetAwaiter().GetResult(); }),
    ("mq pre-parsed", "Count",
        () => QueryEngine.Execute(parsedDoc, QueryParser.Parse("count"))),
    ("json parse+query", "Count",
        () => JsonDocument.Parse(jsonText).RootElement.GetArrayLength()),
    ("json pre-parsed", "Count",
        () => parsedJsonDoc.RootElement.GetArrayLength()),

    // --- Filter (LTS) ---
    ("mq string", "Filter",
        () => QueryEngine.Execute(mdText, "where .Type == \"LTS\" | count")),
    ("mq MemoryStream", "Filter",
        () => { using var ms = new MemoryStream(mdBytes); QueryEngine.ExecuteAsync(ms, "where .Type == \"LTS\" | count").GetAwaiter().GetResult(); }),
    ("mq FileStream", "Filter",
        () => { using var fs = File.OpenRead(mdFilePath); QueryEngine.ExecuteAsync(fs, "where .Type == \"LTS\" | count").GetAwaiter().GetResult(); }),
    ("mq pre-parsed", "Filter",
        () => QueryEngine.Execute(parsedDoc, QueryParser.Parse("where .Type == \"LTS\" | count"))),
    ("json parse+query", "Filter",
        () =>
        {
            int c = 0;
            foreach (var el in JsonDocument.Parse(jsonText).RootElement.EnumerateArray())
                if (el.GetProperty("Type").GetString() == "LTS") c++;
        }),
    ("json pre-parsed", "Filter",
        () =>
        {
            int c = 0;
            foreach (var el in parsedJsonDoc.RootElement.EnumerateArray())
                if (el.GetProperty("Type").GetString() == "LTS") c++;
        }),

    // --- Scalar extract ---
    ("mq string", "Scalar",
        () => QueryEngine.Execute(mdText, ".[0].Version")),
    ("mq MemoryStream", "Scalar",
        () => { using var ms = new MemoryStream(mdBytes); QueryEngine.ExecuteAsync(ms, ".[0].Version").GetAwaiter().GetResult(); }),
    ("mq FileStream", "Scalar",
        () => { using var fs = File.OpenRead(mdFilePath); QueryEngine.ExecuteAsync(fs, ".[0].Version").GetAwaiter().GetResult(); }),
    ("mq pre-parsed", "Scalar",
        () => QueryEngine.Execute(parsedDoc, QueryParser.Parse(".[0].Version"))),
    ("json parse+query", "Scalar",
        () => JsonDocument.Parse(jsonText).RootElement[0].GetProperty("Version").GetString()),
    ("json pre-parsed", "Scalar",
        () => parsedJsonDoc.RootElement[0].GetProperty("Version").GetString()),

    // --- Select columns ---
    ("mq string", "Project",
        () => QueryEngine.Execute(mdText, "select .Version, .Type")),
    ("mq MemoryStream", "Project",
        () => { using var ms = new MemoryStream(mdBytes); QueryEngine.ExecuteAsync(ms, "select .Version, .Type").GetAwaiter().GetResult(); }),
    ("mq FileStream", "Project",
        () => { using var fs = File.OpenRead(mdFilePath); QueryEngine.ExecuteAsync(fs, "select .Version, .Type").GetAwaiter().GetResult(); }),
    ("mq pre-parsed", "Project",
        () => QueryEngine.Execute(parsedDoc, QueryParser.Parse("select .Version, .Type"))),
    ("json parse+query", "Project",
        () =>
        {
            foreach (var el in JsonDocument.Parse(jsonText).RootElement.EnumerateArray())
            {
                _ = el.GetProperty("Version").GetString();
                _ = el.GetProperty("Type").GetString();
            }
        }),
    ("json pre-parsed", "Project",
        () =>
        {
            foreach (var el in parsedJsonDoc.RootElement.EnumerateArray())
            {
                _ = el.GetProperty("Version").GetString();
                _ = el.GetProperty("Type").GetString();
            }
        }),
};

string? currentCategory = null;
foreach (var (label, category, action) in benchmarks)
{
    if (category != currentCategory)
    {
        if (currentCategory is not null) Console.WriteLine();
        Console.WriteLine($"  --- {category} ---");
        currentCategory = category;
    }

    // Warmup
    for (int i = 0; i < WarmupIterations; i++)
        action();

    // Measure
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < MeasureIterations; i++)
        action();
    sw.Stop();

    double usPerOp = sw.Elapsed.TotalMicroseconds / MeasureIterations;
    Console.WriteLine($"    {label,-20} {usPerOp,8:F2} µs/op");
}

// CLI process benchmark — native mq vs native jq
Console.WriteLine();
Console.WriteLine("=== CLI Process Benchmark (native binaries) ===");
Console.WriteLine();

string? mqBin = FindBinary("mq", repoRoot);
string? jqBin = FindBinary("jq", repoRoot);

if (mqBin is null)
    Console.WriteLine("  mq native binary not found — run: dotnet publish tools/mq -c Release");
else if (jqBin is null)
    Console.WriteLine("  jq not found in PATH");
else
{
    const int CliIterations = 500;
    Console.WriteLine($"  mq: {mqBin}");
    Console.WriteLine($"  jq: {jqBin}");
    Console.WriteLine($"  Iterations: {CliIterations}");
    Console.WriteLine();

    string mdPath = Path.Combine(repoRoot, "tests", "mq-bench", "releases.md");
    string jsonPath = Path.Combine(repoRoot, "tests", "mq-bench", "releases.json");

    var cliTests = new (string Label, string Tool, string Args)[]
    {
        ("mq count", mqBin, $"count {mdPath}"),
        ("jq count", jqBin, $"length {jsonPath}"),
        ("mq scalar", mqBin, $".[0].Version {mdPath}"),
        ("jq scalar", jqBin, $".[0].Version {jsonPath}"),
        ("mq filter", mqBin, $"where .Type == \"LTS\" | count {mdPath}"),
        ("jq filter", jqBin, $"[.[] | select(.Type == \"LTS\")] | length {jsonPath}"),
    };

    // Warmup each
    foreach (var (_, tool, cliArgs) in cliTests)
        RunProcess(tool, cliArgs);

    foreach (var (label, tool, cliArgs) in cliTests)
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < CliIterations; i++)
            RunProcess(tool, cliArgs);
        sw.Stop();

        double msPerOp = sw.Elapsed.TotalMilliseconds / CliIterations;
        Console.WriteLine($"    {label,-20} {msPerOp,8:F2} ms/op");
    }
}

Console.WriteLine();
Console.WriteLine($"=== Data sizes ===");
Console.WriteLine($"  JSON: {jsonText.Length} bytes");
Console.WriteLine($"  MD:   {mdText.Length} bytes ({100.0 * mdText.Length / jsonText.Length:F0}% of JSON)");

// === FieldDocument vs FieldParser vs JsonDocument (cache file scenario) ===
Console.WriteLine();
Console.WriteLine("=== FieldDocument vs FieldParser vs JsonDocument (cache lookup) ===");
Console.WriteLine();

string pkgMdPath = Path.Combine(repoRoot, "tests", "mq-bench", "package.md");
string pkgJsonPath = Path.Combine(repoRoot, "tests", "mq-bench", "package.json");
string pkgMdText = File.ReadAllText(pkgMdPath);
byte[] pkgMdBytes = File.ReadAllBytes(pkgMdPath);
string pkgJsonText = File.ReadAllText(pkgJsonPath);
byte[] pkgJsonBytes = File.ReadAllBytes(pkgJsonPath);

Console.WriteLine($"  Package MD: {pkgMdBytes.Length} bytes, JSON: {pkgJsonBytes.Length} bytes");
Console.WriteLine();

// Correctness check
Console.WriteLine("  --- Correctness ---");
{
    using var fdoc = FieldDocument.Parse(pkgMdBytes);
    var fpDict = FieldParser.ParseToDictionary(pkgMdText);
    var jdoc = JsonDocument.Parse(pkgJsonText);

    Console.WriteLine($"    FieldDocument packageName: {fdoc.GetString("packageName")}");
    Console.WriteLine($"    FieldParser   packageName: {fpDict["packageName"].Text}");
    Console.WriteLine($"    JsonDocument  packageName: {jdoc.RootElement.GetProperty("packageName").GetString()}");
    Console.WriteLine($"    FieldDocument assemblyCount: {fdoc.GetInt32("assemblyCount")}");
    Console.WriteLine($"    FieldDocument hasReadme: {fdoc.GetBool("hasReadme")}");
    Console.WriteLine($"    FieldDocument targetFrameworks: [{string.Join(", ", fdoc.GetArray("targetFrameworks") ?? [])}]");
    Console.WriteLine($"    FieldParser   targetFrameworks: [{string.Join(", ", fpDict["targetFrameworks"].Items)}]");
}
Console.WriteLine();

var fieldBenchmarks = new (string Label, string Category, Action Action)[]
{
    // --- Full parse + scalar lookup ---
    ("FieldDocument byte[]", "Parse+Lookup",
        () =>
        {
            using var doc = FieldDocument.Parse(pkgMdBytes);
            _ = doc.GetString("packageName");
            _ = doc.GetString("version");
            _ = doc.GetBool("hasReadme");
            _ = doc.GetInt32("assemblyCount");
        }),
    ("FieldParser string", "Parse+Lookup",
        () =>
        {
            var dict = FieldParser.ParseToDictionary(pkgMdText);
            _ = dict["packageName"].Text;
            _ = dict["version"].Text;
            _ = dict["hasReadme"].Text;
            _ = dict["assemblyCount"].Text;
        }),
    ("JsonDocument string", "Parse+Lookup",
        () =>
        {
            using var doc = JsonDocument.Parse(pkgJsonText);
            _ = doc.RootElement.GetProperty("packageName").GetString();
            _ = doc.RootElement.GetProperty("version").GetString();
            _ = doc.RootElement.GetProperty("hasReadme").GetBoolean();
            _ = doc.RootElement.GetProperty("assemblyCount").GetInt32();
        }),
    ("JsonDocument byte[]", "Parse+Lookup",
        () =>
        {
            using var doc = JsonDocument.Parse(pkgJsonBytes);
            _ = doc.RootElement.GetProperty("packageName").GetString();
            _ = doc.RootElement.GetProperty("version").GetString();
            _ = doc.RootElement.GetProperty("hasReadme").GetBoolean();
            _ = doc.RootElement.GetProperty("assemblyCount").GetInt32();
        }),

    // --- Full hydrate (all fields like PackageIndexCache does) ---
    ("FieldDocument hydrate", "Full Hydrate",
        () =>
        {
            using var doc = FieldDocument.Parse(pkgMdBytes);
            _ = doc.GetString("packageName");
            _ = doc.GetString("version");
            _ = doc.GetString("description");
            _ = doc.GetString("authors");
            _ = doc.GetString("license");
            _ = doc.GetString("repository");
            _ = doc.GetString("readmeFile");
            _ = doc.GetBool("hasReadme");
            _ = doc.GetBool("isToolPackage");
            _ = doc.GetInt32("assemblyCount");
            _ = doc.GetBool("isFrameworkDependent");
            _ = doc.GetBool("hasRidSpecificAssets");
            _ = doc.GetBool("hasNativeDependencies");
            _ = doc.GetBool("isRidSpecificPointerPackage");
            _ = doc.GetString("toolFormat");
            _ = doc.GetString("runtimeTargetRid");
            _ = doc.GetArray("packageTypes");
            _ = doc.GetArray("targetFrameworks");
            _ = doc.GetArray("dependencyGroups");
        }),
    ("FieldParser hydrate", "Full Hydrate",
        () =>
        {
            var dict = FieldParser.ParseToDictionary(pkgMdText);
            _ = dict.GetValueOrDefault("packageName").Text;
            _ = dict.GetValueOrDefault("version").Text;
            _ = dict.GetValueOrDefault("description").Text;
            _ = dict.GetValueOrDefault("authors").Text;
            _ = dict.GetValueOrDefault("license").Text;
            _ = dict.GetValueOrDefault("repository").Text;
            _ = dict.GetValueOrDefault("readmeFile").Text;
            _ = dict.GetValueOrDefault("hasReadme").Text;
            _ = dict.GetValueOrDefault("isToolPackage").Text;
            _ = dict.GetValueOrDefault("assemblyCount").Text;
            _ = dict.GetValueOrDefault("isFrameworkDependent").Text;
            _ = dict.GetValueOrDefault("hasRidSpecificAssets").Text;
            _ = dict.GetValueOrDefault("hasNativeDependencies").Text;
            _ = dict.GetValueOrDefault("isRidSpecificPointerPackage").Text;
            _ = dict.GetValueOrDefault("toolFormat").Text;
            _ = dict.GetValueOrDefault("runtimeTargetRid").Text;
            _ = dict.GetValueOrDefault("packageTypes").Items;
            _ = dict.GetValueOrDefault("targetFrameworks").Items;
            _ = dict.GetValueOrDefault("dependencyGroups").Items;
        }),
    ("JsonDocument hydrate", "Full Hydrate",
        () =>
        {
            using var doc = JsonDocument.Parse(pkgJsonBytes);
            var root = doc.RootElement;
            _ = root.GetProperty("packageName").GetString();
            _ = root.GetProperty("version").GetString();
            _ = root.GetProperty("description").GetString();
            _ = root.GetProperty("authors").GetString();
            _ = root.GetProperty("license").GetString();
            _ = root.GetProperty("repository").GetString();
            _ = root.GetProperty("readmeFile").GetString();
            _ = root.GetProperty("hasReadme").GetBoolean();
            _ = root.GetProperty("isToolPackage").GetBoolean();
            _ = root.GetProperty("assemblyCount").GetInt32();
            _ = root.GetProperty("isFrameworkDependent").GetBoolean();
            _ = root.GetProperty("hasRidSpecificAssets").GetBoolean();
            _ = root.GetProperty("hasNativeDependencies").GetBoolean();
            _ = root.GetProperty("isRidSpecificPointerPackage").GetBoolean();
            _ = root.GetProperty("packageTypes").EnumerateArray().Select(x => x.GetString()).ToArray();
            _ = root.GetProperty("targetFrameworks").EnumerateArray().Select(x => x.GetString()).ToArray();
            _ = root.GetProperty("dependencyGroups").EnumerateArray().Select(x => x.GetString()).ToArray();
        }),

    // --- File.ReadAllBytes + parse (end-to-end I/O) ---
    ("ReadAllBytes+FDoc", "File I/O",
        () =>
        {
            var bytes = File.ReadAllBytes(pkgMdPath);
            using var doc = FieldDocument.Parse(bytes);
            _ = doc.GetString("packageName");
            _ = doc.GetString("version");
        }),
    ("ReadAllText+FParser", "File I/O",
        () =>
        {
            var text = File.ReadAllText(pkgMdPath);
            var dict = FieldParser.ParseToDictionary(text);
            _ = dict["packageName"].Text;
            _ = dict["version"].Text;
        }),
    ("ReadAllBytes+Json", "File I/O",
        () =>
        {
            var bytes = File.ReadAllBytes(pkgJsonPath);
            using var doc = JsonDocument.Parse(bytes);
            _ = doc.RootElement.GetProperty("packageName").GetString();
            _ = doc.RootElement.GetProperty("version").GetString();
        }),
    ("ReadAllText+Json", "File I/O",
        () =>
        {
            var text = File.ReadAllText(pkgJsonPath);
            using var doc = JsonDocument.Parse(text);
            _ = doc.RootElement.GetProperty("packageName").GetString();
            _ = doc.RootElement.GetProperty("version").GetString();
        }),
};

currentCategory = null;
foreach (var (label, category, action) in fieldBenchmarks)
{
    if (category != currentCategory)
    {
        if (currentCategory is not null) Console.WriteLine();
        Console.WriteLine($"  --- {category} ---");
        currentCategory = category;
    }

    for (int i = 0; i < WarmupIterations; i++)
        action();

    var sw2 = Stopwatch.StartNew();
    for (int i = 0; i < MeasureIterations; i++)
        action();
    sw2.Stop();

    double usPerOp = sw2.Elapsed.TotalMicroseconds / MeasureIterations;
    Console.WriteLine($"    {label,-24} {usPerOp,8:F2} µs/op");
}

static void RunProcess(string fileName, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    });
    p!.WaitForExit();
}

static string? FindBinary(string name, string repoRoot)
{
    // Check publish output first (native AOT)
    string publishPath = Path.Combine(repoRoot, "artifacts", "publish", name, "release", name);
    if (File.Exists(publishPath)) return publishPath;

    // Fall back to PATH
    using var p = Process.Start(new ProcessStartInfo
    {
        FileName = "which",
        Arguments = name,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    });
    string? path = p?.StandardOutput.ReadLine();
    p?.WaitForExit();
    return p?.ExitCode == 0 ? path : null;
}

static string Truncate(string s)
{
    var line = s.Split('\n')[0];
    return line.Length > 40 ? line[..40] + "..." : line;
}
