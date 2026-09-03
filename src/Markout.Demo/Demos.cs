using Markout;
using Markout.Ansi.Spectre;
using Markout.Formatting;
using Spectre.Console;

namespace Markout.Demo;

/// <summary>
/// Registry of available demos.
/// </summary>
public static class Demos
{
    private static readonly Dictionary<string, Action<TextWriter>> _demos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["simple"] = Simple,
        ["sections"] = Sections,
        ["list"] = ListDemo,
        ["table"] = TableDemo,
        ["nested"] = Nested,
        ["pivot"] = Pivot,
        ["tree"] = Tree,
        ["textdiff"] = TextDiff,
        ["textdiffil"] = TextDiffIl,
        ["textdiffjsonl"] = TextDiffJsonl,
        ["textdiffplain"] = TextDiffPlain,
        ["textdiffpretty"] = TextDiffPretty,
        ["textdiffspectre"] = TextDiffSpectre,
        ["textdifftsv"] = TextDiffTsv,
        ["textdiffunicode"] = TextDiffUnicode,
        ["schema"] = Schema,
    };

    private static readonly string[] _orderedNames =
    [
        "simple",
        "sections",
        "list",
        "table",
        "nested",
        "pivot",
        "tree",
        "textdiff",
        "textdiffplain",
        "textdiffpretty",
        "textdifftsv",
        "textdiffjsonl",
        "textdiffunicode",
        "textdiffspectre",
        "textdiffil",
        "schema"
    ];

    public static IEnumerable<string> List() => _orderedNames;

    public static Action<TextWriter>? Get(string name) =>
        _demos.TryGetValue(name, out var demo) ? demo : null;

    /// <summary>
    /// Simple demo: Single shoe with basic scalar fields.
    /// </summary>
    private static void Simple(TextWriter output)
    {
        var data = DemoData.GetSimpleShoe("torin-7");
        MarkoutSerializer.Serialize(data, output, DemoContext.Default);
    }

    /// <summary>
    /// List demo: All shoes as bullet list.
    /// </summary>
    private static void ListDemo(TextWriter output)
    {
        var data = DemoData.GetShoeBulletList();
        MarkoutSerializer.Serialize(data, output, DemoContext.Default);
    }

    /// <summary>
    /// Table demo: All shoes as table rows.
    /// </summary>
    private static void TableDemo(TextWriter output)
    {
        var data = DemoData.GetShoeTable();
        MarkoutSerializer.Serialize(data, output, DemoContext.Default);
    }

    /// <summary>
    /// Nested demo: Shoes with detailed features and reviews.
    /// </summary>
    private static void Nested(TextWriter output)
    {
        var data = DemoData.GetShoeDetail();
        MarkoutSerializer.Serialize(data, output, DemoContext.Default);
    }

    /// <summary>
    /// Sections demo: Single shoe with specs and reviews sections.
    /// </summary>
    private static void Sections(TextWriter output)
    {
        var data = DemoData.GetShoeSections("lone-peak-8");
        MarkoutSerializer.Serialize(data, output, DemoContext.Default);
    }

    /// <summary>
    /// Pivot demo: Inventory pivoted by size and color.
    /// </summary>
    private static void Pivot(TextWriter output)
    {
        var data = DemoData.GetShoeInventory("torin-7");
        MarkoutSerializer.Serialize(data, output, DemoContext.Default);
    }

    /// <summary>
    /// Schema demo: Shows how types map to Markout output.
    /// </summary>
    private static void Schema(TextWriter output)
    {
        output.WriteLine("# Markout Schema");
        output.WriteLine();
        
        // Shoe
        var shoeSchema = DemoContext.Default.GetSchemaInfo<Shoe>();
        if (shoeSchema != null)
        {
            output.WriteLine("## Shoe");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(shoeSchema.ToTreeString());
            output.WriteLine("```");
            output.WriteLine();
        }
        
        // InventoryEntry
        var inventorySchema = DemoContext.Default.GetSchemaInfo<InventoryEntry>();
        if (inventorySchema != null)
        {
            output.WriteLine("## InventoryEntry");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(inventorySchema.ToTreeString());
            output.WriteLine("```");
            output.WriteLine();
        }
        
        // Feature
        var featureSchema = DemoContext.Default.GetSchemaInfo<Feature>();
        if (featureSchema != null)
        {
            output.WriteLine("## Feature");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(featureSchema.ToTreeString());
            output.WriteLine("```");
            output.WriteLine();
        }
        
        // Review
        var reviewSchema = DemoContext.Default.GetSchemaInfo<Review>();
        if (reviewSchema != null)
        {
            output.WriteLine("## Review");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(reviewSchema.ToTreeString());
            output.WriteLine("```");
        }
    }

    /// <summary>
    /// Tree demo: Shows nested list data as a tree structure.
    /// </summary>
    private static void Tree(TextWriter output)
    {
        var shoes = DemoData.GetShoesForTree("torin-7", "escalante-4", "lone-peak-8");

        // Project to TreeNode structure
        var tree = shoes.Select(s => new TreeNode(
            $"{s.Model} ({s.Category}, ${s.Price})",
            s.Reviews?.Select(r =>
            {
                var comment = r.Comment.Length > 40
                    ? r.Comment.Substring(0, 37) + "..."
                    : r.Comment;
                return new TreeNode($"\"{comment}\" — {r.Author} ({r.Rating}★)");
            })));

        var writer = new MarkoutWriter(output, new MarkdownFormatter());
        
        writer.WriteHeading(1, "Altra Running Shoes");
        writer.WriteParagraph("This demo shows `List<List<T>>` rendered as a tree. Each shoe shows its reviews, which would be unsupported in table format.");
        
        writer.WriteHeading(2, "Products with Reviews");
        writer.WriteCodeStart();
        writer.WriteTree(tree.ToArray());
        writer.WriteCodeEnd();
        writer.Flush();
    }

    /// <summary>Mapped text diff demo: GNU-compatible Markdown lowering.</summary>
    private static void TextDiff(TextWriter output)
    {
        var writer = new MarkoutWriter(
            output,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0 });
        writer.WriteHeading(1, "Mapped Text Diff");
        writer.WriteTextDiff(SampleDiff());
        writer.Flush();
    }

    /// <summary>Mapped text diff demo: GNU-compatible plain-text lowering.</summary>
    private static void TextDiffPlain(TextWriter output)
    {
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — Plain Text",
            "diff",
            SampleDiff(),
            new PlainTextFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0 });
    }

    /// <summary>Mapped text diff demo: structured pretty-table provenance records.</summary>
    private static void TextDiffPretty(TextWriter output)
    {
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — Pretty Table",
            "text",
            SampleDiff(),
            new TableFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0 });
    }

    /// <summary>Mapped text diff demo: structured TSV provenance records.</summary>
    private static void TextDiffTsv(TextWriter output)
    {
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — TSV",
            "tsv",
            SampleDiff(),
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Tsv,
                TextDiffContextLines = 0
            },
            static text => text.Replace("\t", "\\t", StringComparison.Ordinal));
    }

    /// <summary>Mapped text diff demo: structured JSONL provenance records.</summary>
    private static void TextDiffJsonl(TextWriter output)
    {
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — JSONL",
            "jsonl",
            SampleDiff(),
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                OmitEmptyJsonFields = true,
                TextDiffContextLines = 0
            });
    }

    /// <summary>Mapped text diff demo: the same shape applied to IL instructions.</summary>
    private static void TextDiffIl(TextWriter output)
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["IL_0000: ldc.i4.0", "IL_0001: ret"], "Before IL"),
            new TextDiffSequence(["IL_0000: ldc.i4.1", "IL_0001: ret"], "After IL"),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — IL",
            "text",
            diff,
            new UnicodeFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 1 });
    }

    /// <summary>Mapped text diff demo: rich Unicode terminal lowering.</summary>
    private static void TextDiffUnicode(TextWriter output)
    {
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — Unicode",
            "text",
            SampleDiff(),
            new UnicodeFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0 });
    }

    /// <summary>Mapped text diff demo: Spectre ANSI terminal lowering.</summary>
    private static void TextDiffSpectre(TextWriter output)
    {
        WriteEmbeddedDiff(
            output,
            "Mapped Text Diff — Spectre ANSI",
            "text",
            SampleDiff(),
            new SpectreFormatter(AnsiConsole.Console),
            new MarkoutWriterOptions { TextDiffContextLines = 0 },
            static text => text.Replace("\u001b", "\\e", StringComparison.Ordinal));
    }

    private static void WriteEmbeddedDiff(
        TextWriter output,
        string title,
        string language,
        MappedTextDiff diff,
        IMarkoutFormatter formatter,
        MarkoutWriterOptions options,
        Func<string, string>? prepareForCodeFence = null)
    {
        var rendered = MarkoutWriter.Create(formatter, options);
        rendered.WriteTextDiff(diff);

        var document = new MarkoutWriter(output, new MarkdownFormatter());
        document.WriteHeading(1, title);
        document.WriteCodeStart(language);
        var text = rendered.ToString();
        document.WriteParagraph(prepareForCodeFence is null ? text : prepareForCodeFence(text));
        document.WriteCodeEnd();
        document.Flush();
    }

    private static MappedTextDiff SampleDiff() => new(
        new TextDiffSequence(
            ["if (value < 0)", "    return 0;", "", "Process(value);", "return value;"],
            "Before",
            TextDiffLineTerminator.Present),
        new TextDiffSequence(
            ["if (value <= 0)", "    return 1;", "", "Process(value);", "Log(value);", "return value;"],
            "After",
            TextDiffLineTerminator.Present),
        [
            new TextDiffChange(
                new TextDiffRange(0, 2),
                new TextDiffRange(0, 2),
                [
                    new TextDiffInnerMapping(
                        new TextDiffSpan(0, 10, 1),
                        new TextDiffSpan(0, 10, 2)),
                    new TextDiffInnerMapping(
                        new TextDiffSpan(1, 11, 1),
                        new TextDiffSpan(1, 11, 1))
                ],
                [
                    TextDiffAnnotation.ForSpan(
                        TextDiffSide.After,
                        new TextDiffSpan(0, 10, 2),
                        "Boundary now includes zero"),
                    TextDiffAnnotation.ForSpan(
                        TextDiffSide.After,
                        new TextDiffSpan(1, 11, 1),
                        "Branch now returns one")
                ]),
            new TextDiffChange(
                new TextDiffRange(4, 0),
                new TextDiffRange(4, 1),
                annotations:
                [
                    TextDiffAnnotation.ForLine(
                        TextDiffSide.After,
                        4,
                        "Processed values are now logged")
                ])
        ]);
}
