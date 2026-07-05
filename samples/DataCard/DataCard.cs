using Markout;

// A "data card": one annotated model that renders as a dense, human-readable Markdown
// table AND decomposes into typed columns in JSONL/TSV — from a single declaration, with
// no pre-stringifying. Here we compare a baseline build to a release candidate.
//
// Each property is a composite cell:
//   Change<V>            a before -> after change ([MarkoutDelta] adds a derived % or delta)
//   Fraction/Share/      count/total, value (% of a whole), a percent, or independent
//   Percent/Segments     slash-joined parts — the type picks the rendering.

var card = new BuildCard
{
    Tests = new(new Fraction(338, 342), new Fraction(342, 342)),
    Warnings = new(
        new Segments(new Segment("build", 12), new Segment("analyzer", 8), new Segment("style", 3)),
        new Segments(new Segment("build", 4), new Segment("analyzer", 2), new Segment("style", 0))),
    Coverage = new(new Percent(78, 100), new Percent(84, 100)),
    Startup = new(new Share(420, 500), new Share(180, 500)),
    BinarySize = new(1_048_576, 983_040),
    Verdict = "SHIP IT",
};

// 1) Markdown — the dense card a human reads.
Console.WriteLine("# Markdown\n");
Console.WriteLine(MarkoutSerializer.Serialize(card, BuildCardContext.Default));

// 2) JSONL — the SAME rows, decomposed into typed columns a tool can consume.
//    OmitEmptyJsonFields makes each record carry only its own keys; JsonTypedValues emits numbers.
Console.WriteLine("\n# JSONL (same model, decomposed)\n");
MarkoutSerializer.Serialize(card, Console.Out, new TableFormatter(), BuildCardContext.Default,
    new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true, OmitEmptyJsonFields = true });

[MarkoutSerializable]
public sealed class BuildCard
{
    [MarkoutPropertyName("tests passing")]
    public Change<Fraction> Tests { get; init; }                 // 338/342 -> 342/342

    [MarkoutPropertyName("warnings: build / analyzer / style")]
    public Change<Segments> Warnings { get; init; }              // 12/8/3 -> 4/2/0

    [MarkoutPropertyName("line coverage")]
    public Change<Percent> Coverage { get; init; }               // 78% -> 84%

    [MarkoutPropertyName("startup (% of budget)"), MarkoutUnit("ms")]
    public Change<Share> Startup { get; init; }                  // 420ms (84%) -> 180ms (36%)

    [MarkoutPropertyName("binary size (bytes)"), MarkoutDelta(Delta.Percent)]
    public Change<long> BinarySize { get; init; }                // 1048576 -> 983040 (-6%)

    public string Verdict { get; init; } = "";                   // SHIP IT
}

[MarkoutContext(typeof(BuildCard))]
public partial class BuildCardContext : MarkoutSerializerContext { }
