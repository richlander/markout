using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[MarkoutSerializable]
public class TableContainer
{
    [MarkoutSection(Name = "Metadata: Image", EmptyText = "No metadata found.")]
    public MarkoutTable? Image { get; set; }
}

[MarkoutContext(typeof(TableContainer))]
public partial class TableContext : MarkoutSerializerContext
{
}

// A model element whose section name is a runtime value, carrying a runtime-column table body.
// Unwrapped as a top-level list, each element becomes its own level-2 section — the "dynamic set
// of named sections" capability — and its table participates in ordering, windowing, and
// projection like any generated section. The title is [MarkoutIgnore]d so the runtime name drives
// only the heading and is not repeated as a field row; the table is [MarkoutIgnoreInTable] because
// a shape cannot be a table cell.
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class DynamicMetadataSection
{
    [MarkoutIgnore]
    public string Name { get; set; } = "";

    [MarkoutIgnoreInTable]
    public MarkoutTable? Body { get; set; }
}

[MarkoutSerializable]
public class DynamicMetadataDocument
{
    [MarkoutUnwrap]
    public List<DynamicMetadataSection> Sections { get; set; } = [];
}

[MarkoutContext(typeof(DynamicMetadataDocument))]
public partial class DynamicMetadataContext : MarkoutSerializerContext
{
}

/// <summary>
/// A <see cref="MarkoutTable"/>-typed model property — a table whose columns are runtime data —
/// must reach <see cref="MarkoutWriter.WriteTable(MarkoutTable)"/> through the generated
/// serializer, so a host that models its document declaratively earns section ordering, row
/// windowing, column projection, inclusion filtering, and structured decomposition for free
/// instead of hand-writing the table and re-earning each feature.
/// </summary>
public class GeneratedTableTests
{
    private static MarkoutTable ImageTable() => new(
        ["Property", "Value"],
        [
            ["Machine", "Amd64"],
            ["Characteristics", "ExecutableImage"],
            ["Subsystem", "WindowsCui"],
        ]);

    private static TableContainer Sample() => new() { Image = ImageTable() };

    [Fact]
    public void TableProperty_RendersAsASectionInMarkdown()
    {
        var mdf = MarkoutSerializer.Serialize(Sample(), TableContext.Default);

        Assert.Contains("## Metadata: Image", mdf, StringComparison.Ordinal);
        Assert.Contains("| Property | Value |", mdf, StringComparison.Ordinal);
        Assert.Contains("| Machine | Amd64 |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("No metadata found.", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyTable_RendersTheSectionHeadingWithItsEmptyText()
    {
        var model = new TableContainer { Image = new MarkoutTable([], []) };

        var mdf = MarkoutSerializer.Serialize(model, TableContext.Default);

        Assert.Contains("## Metadata: Image", mdf, StringComparison.Ordinal);
        Assert.Contains("No metadata found.", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void NullTable_OmitsTheSectionEntirely()
    {
        var model = new TableContainer { Image = null };

        var mdf = MarkoutSerializer.Serialize(model, TableContext.Default);

        Assert.DoesNotContain("Metadata: Image", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("No metadata found.", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_DecomposesToTsvKeyedByHeaderNames()
    {
        var model = new TableContainer
        {
            Image = new MarkoutTable(["Property", "Value"], ["prop", "val"], [["Machine", "Amd64"]]),
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(model, sw, new TableFormatter(), TableContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        var tsv = sw.ToString();

        Assert.Contains("prop\tval", tsv, StringComparison.Ordinal);
        Assert.Contains("Machine\tAmd64", tsv, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_DecomposesToJsonlKeyedByHeaderNames()
    {
        var model = new TableContainer
        {
            Image = new MarkoutTable(["Property", "Value"], ["prop", "val"], [["Machine", "Amd64"]]),
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(model, sw, new TableFormatter(), TableContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        var jsonl = sw.ToString();

        Assert.Contains("{\"prop\":\"Machine\",\"val\":\"Amd64\"}", jsonl, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_HonorsRowWindow()
    {
        var options = new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Head(1) };
        var mdf = MarkoutSerializer.Serialize(Sample(), TableContext.Default, options);

        Assert.Contains("| Machine | Amd64 |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("Subsystem", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_ProjectsColumnsThatMatch()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["Value"] },
        };
        var mdf = MarkoutSerializer.Serialize(Sample(), TableContext.Default, options);

        Assert.Contains("| Value |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("| Property | Value |", mdf, StringComparison.Ordinal);
        Assert.Contains("| Amd64 |", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_ThrowsWhenTheProjectionMatchesNothingInTheDocument()
    {
        // A projection is an allow list, so an individual table matching none of it contributes
        // nothing -- see the sibling-section test below, which is what that rule exists for. But a
        // projection matching nothing in the *whole* document names columns this document does not
        // have, which is a caller error, and rendering an empty document for it would turn that
        // error into success-shaped empty output.
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["ColumnThatDoesNotExist"] },
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => MarkoutSerializer.Serialize(Sample(), TableContext.Default, options));
        Assert.Contains("No columns matched projection: ColumnThatDoesNotExist", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_ProjectionMatchingASiblingSectionLeavesEachSectionItsOwnColumns()
    {
        // The case the rule exists for: one projection, two sections with different columns.
        // The matching section projects; the non-matching one drops out rather than throwing.
        var model = new DynamicMetadataDocument
        {
            Sections =
            [
                new() { Name = "Alpha", Body = new MarkoutTable(["Only Alpha"], [["a"]]) },
                new() { Name = "Beta", Body = new MarkoutTable(["Only Beta"], [["b"]]) },
            ],
        };
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["Only Beta"] },
        };
        var mdf = MarkoutSerializer.Serialize(model, DynamicMetadataContext.Default, options);

        Assert.Contains("| Only Beta |", mdf, StringComparison.Ordinal);
        Assert.Contains("| b |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("Only Alpha", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("| a |", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_ProjectionMatchesTheCanonicalKeyStructuredOutputEmits()
    {
        // Without explicit header names, TSV/JSONL key on snake_case(display header). A projection
        // copied from that output must therefore match, or the caller silently gets the wrong
        // columns from the very names the tool printed.
        var model = new TableContainer
        {
            Image = new MarkoutTable(["Display A", "Display B"], [["a", "b"]]),
        };
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["display_b"] },
        };
        var mdf = MarkoutSerializer.Serialize(model, TableContext.Default, options);

        Assert.Contains("| Display B |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("Display A", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void TableProperty_EscapesHeadersTheSameWayItEscapesCells()
    {
        // A MarkoutTable's headers are runtime data. An unescaped pipe would silently add a
        // column and an unescaped newline would end the table mid-header.
        var model = new TableContainer
        {
            Image = new MarkoutTable(["A|B", "C\nD", "<img src=x>"], [["1", "2", "3"]]),
        };
        var mdf = MarkoutSerializer.Serialize(model, TableContext.Default);

        var headerLine = mdf.Split('\n').First(l => l.Contains("A", StringComparison.Ordinal) && l.StartsWith('|'));
        Assert.Equal(4, headerLine.Count(c => c == '|')); // 3 columns => 4 pipes, not 5
        Assert.Contains("&#124;", headerLine, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkoutTable_RejectsRowsThatDoNotMatchTheHeaderCount()
    {
        // Every renderer indexes rows positionally against the headers, and they disagree about a
        // mismatch: Markdown and TSV keep an extra cell, JSONL drops it. Fail at construction.
        var shortRow = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["A", "B"], [["x"]]));
        Assert.Contains("Row 0 has 1 cell(s) but the table has 2 column(s)", shortRow.Message, StringComparison.Ordinal);

        var longRow = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["A", "B"], [["x", "y", "z"]]));
        Assert.Contains("Row 0 has 3 cell(s) but the table has 2 column(s)", longRow.Message, StringComparison.Ordinal);

        var nullRow = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["A"], [null!]));
        Assert.Contains("Row 0 is null", nullRow.Message, StringComparison.Ordinal);

        // The well-formed case still constructs, including zero rows.
        _ = new MarkoutTable(["A", "B"], [["x", "y"]]);
        _ = new MarkoutTable(["A", "B"], []);
    }

    [Fact]
    public void MarkoutTable_RejectsColumnsThatShareACanonicalStructuredKey()
    {
        // "A-B" and "A B" both canonicalize to a_b, which would emit JSONL with duplicate keys —
        // and a consumer recovers only the last, silently losing a column.
        var ex = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["A-B", "A B"], [["one", "two"]]));
        Assert.Contains("canonical structured key 'a_b'", ex.Message, StringComparison.Ordinal);

        // Distinct keys are unaffected.
        _ = new MarkoutTable(["Line", "End Line"], [["1", "2"]]);
    }

    // ---- Dynamic set of named sections, each carrying a runtime-column table ----

    private static DynamicMetadataDocument DynamicSample() => new()
    {
        Sections =
        [
            new DynamicMetadataSection
            {
                Name = "Metadata: #Strings",
                Body = new MarkoutTable(["Offset", "Value"],
                    [["0x1", "System"], ["0x8", "Object"], ["0x10", "String"]]),
            },
            new DynamicMetadataSection
            {
                Name = "Metadata: #Blob",
                Body = new MarkoutTable(["Address", "Length"],
                    [["0x0", "12"], ["0xC", "4"]]),
            },
        ],
    };

    [Fact]
    public void UnwrappedTables_EmitOneLevelTwoSectionPerRuntimeElement()
    {
        var mdf = MarkoutSerializer.Serialize(DynamicSample(), DynamicMetadataContext.Default);

        Assert.Contains("## Metadata: #Strings", mdf, StringComparison.Ordinal);
        Assert.Contains("## Metadata: #Blob", mdf, StringComparison.Ordinal);
        Assert.Contains("| Offset | Value |", mdf, StringComparison.Ordinal);
        Assert.Contains("| Address | Length |", mdf, StringComparison.Ordinal);
        // The runtime name drives the heading only, never a stray field row.
        Assert.DoesNotContain("| Name |", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void UnwrappedTables_ReorderWithSectionOrder()
    {
        var options = new MarkoutWriterOptions
        {
            SectionOrder = ["Metadata: #Blob", "Metadata: #Strings"],
        };
        var mdf = MarkoutSerializer.Serialize(DynamicSample(), DynamicMetadataContext.Default, options);

        var blob = mdf.IndexOf("## Metadata: #Blob", StringComparison.Ordinal);
        var strings = mdf.IndexOf("## Metadata: #Strings", StringComparison.Ordinal);
        Assert.True(blob >= 0 && strings >= 0);
        Assert.True(blob < strings, "SectionOrder must move #Blob before #Strings.");
    }

    [Fact]
    public void UnwrappedTables_FilterWithIncludeSections()
    {
        var options = new MarkoutWriterOptions { IncludeSections = ["Metadata: #Blob"] };
        var mdf = MarkoutSerializer.Serialize(DynamicSample(), DynamicMetadataContext.Default, options);

        Assert.Contains("## Metadata: #Blob", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("## Metadata: #Strings", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void UnwrappedTables_WindowRowsPerSection()
    {
        var options = new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Head(1) };
        var mdf = MarkoutSerializer.Serialize(DynamicSample(), DynamicMetadataContext.Default, options);

        Assert.Contains("| 0x1 | System |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("Object", mdf, StringComparison.Ordinal);
        Assert.Contains("| 0x0 | 12 |", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("| 0xC | 4 |", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposedDocument_IsByteIdenticalToOneAssembledByHand()
    {
        // The whole point: a document whose sections come from a MarkoutTable model property must
        // be byte-for-byte what the writer produces writing those same headings and tables
        // directly — no post-processing, no reconciliation.
        var viaModel = MarkoutSerializer.Serialize(DynamicSample(), DynamicMetadataContext.Default);

        var sw = new StringWriter();
        var writer = new MarkoutWriter(sw, new MarkdownFormatter());
        writer.WriteHeading(2, "Metadata: #Strings");
        writer.WriteTable(new MarkoutTable(["Offset", "Value"],
            [["0x1", "System"], ["0x8", "Object"], ["0x10", "String"]]));
        writer.WriteHeading(2, "Metadata: #Blob");
        writer.WriteTable(new MarkoutTable(["Address", "Length"],
            [["0x0", "12"], ["0xC", "4"]]));
        writer.Flush();
        var viaHand = sw.ToString();

        // The serializer finalizes the document with a single trailing newline that the raw writer
        // does not add; every byte of the headings, tables, rows, and separators is otherwise
        // identical. Normalize only that terminator.
        Assert.Equal(viaHand.TrimEnd('\n'), viaModel.TrimEnd('\n'));
    }
}
