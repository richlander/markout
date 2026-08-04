using MarkdownTable.Formatting;
using System.Globalization;
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

    [Fact]
    public void MarkoutTable_ResolvesAnEmptyExplicitNameToTheDisplayHeaderWhenCheckingForDuplicateKeys()
    {
        // TableWriter.FormatHeaders falls back to the display header when an explicit name is
        // null or empty, so validating the raw name list would check a name that never reaches
        // the output: here "" and "A B" look distinct while both render under a_b.
        var ex = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["A-B", "A B"], ["", "A B"], [["one", "two"]]));
        Assert.Contains("canonical structured key 'a_b'", ex.Message, StringComparison.Ordinal);

        // The fallback itself still works where it does not collide.
        _ = new MarkoutTable(["Alpha", "Beta"], ["", "beta_name"], [["1", "2"]]);
    }

    [Fact]
    public void MarkoutTable_RejectsColumnsThatShareADisplayHeader()
    {
        // Under MarkoutTableHeaderStyle.DisplayName the display header IS the structured key, so
        // distinct stable names do not save a table whose display headers repeat: JSONL would
        // carry {"Same":"one","Same":"two"} and a consumer recovers only the last.
        var ex = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["Same", "Same"], ["first", "second"], [["one", "two"]]));
        Assert.Contains("share the display header 'Same'", ex.Message, StringComparison.Ordinal);

        // Distinct display headers are unaffected.
        _ = new MarkoutTable(["Line", "End Line"], ["line", "end_line"], [["1", "2"]]);
    }

    [Fact]
    public void Projection_MatchesTheCanonicalKeyOfAColumnWhoseStableNameFallsBackToItsDisplayHeader()
    {
        // The column's emitted TSV/JSONL key is display_name, because an empty explicit name falls
        // back to the display header. Projection has to accept the key the output actually uses.
        var table = new MarkoutTable(["Display Name"], [""], [["value"]]);

        var tsv = new StringWriter();
        MarkoutWriter.Create(tsv, new TableFormatter(), new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv })
            .WriteTable(table);
        Assert.StartsWith("display_name", tsv.ToString(), StringComparison.Ordinal);

        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["display_name"] }
        });
        writer.WriteTable(table);
        Assert.Contains("Display Name", writer.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0xD800, 0xD801, "")]        // two distinct lone high surrogates
    [InlineData(0xDC00, 0xDFFF, "")]        // two distinct lone low surrogates
    [InlineData(0xD800, 0xD801, "A")]       // and with valid text around them
    public void MarkoutTable_RejectsHeadersThatOnlyDifferOutsideWhatCanBeEncoded(int first, int second, string prefix)
    {
        // Lone surrogates are not encodable, so the UTF-8 encoder substitutes U+FFFD and every
        // distinct one arrives as the same JSONL key: {"\uFFFD":"one","\uFFFD":"two"} loses a column
        // exactly as a literal duplicate would. Rejected for the same reason and by the same rule.
        //
        // The code units are passed as ints and composed here because xUnit's InlineData
        // serialization does not round-trip a lone surrogate: handed the strings directly, both
        // arrive already flattened to U+FFFD and the test proves only that literal duplicates are
        // rejected -- which it did, silently, until a tamper failed to break it.
        //
        // Explicit stable names keep the canonical-key rule out of it, so what is under test is the
        // display-header rule reading what the encoder will actually write.
        var left = prefix + (char)first;
        var right = prefix + (char)second;
        Assert.NotEqual(left, right);

        var ex = Assert.Throws<ArgumentException>(() =>
            new MarkoutTable([left, right], ["x", "y"], [["one", "two"]]));

        Assert.Contains("display header", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkoutTable_AcceptsHeadersThatDifferOnlyOutsideTheBasicPlane()
    {
        // The negative case, and the one a rule about "text containing surrogates" would break:
        // well-formed pairs encode faithfully, stay distinct in every format, and must be accepted
        // even though they are spelled with the same surrogate code units the case above rejects.
        var table = new MarkoutTable(["A\U0001F600", "A\U0001F601"], ["x", "y"], [["one", "two"]]);

        Assert.Equal(2, table.Headers.Count);
    }

    [Fact]
    public void Projection_ATableTheProjectionDrops_DoesNotEnumerateItsRows()
    {
        // A per-table miss renders nothing, and the decision is final the moment the matcher says
        // so. Enumerating the rows anyway is a behaviour change for callers passing sequences that
        // are expensive, infinite, or invalid for a table that was never going to render -- and it
        // is pure waste, since not one of those rows can reach the output.
        var projection = new MarkoutProjection { IncludeColumns = ["Keep"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });
        writer.WriteTable(["Keep"], [["k"]]);

        var enumerated = 0;
        writer.WriteTable(["Dropped"], Counted());

        Assert.Equal(0, enumerated);

        IEnumerable<string[]> Counted()
        {
            enumerated++;
            yield return ["x"];
        }
    }

    [Fact]
    public void Streaming_ATableOpenWhenAnExcludedSectionBegins_IsStillEmitted()
    {
        // A table that started in an included section belongs to that section. If the caller opens
        // an excluded section before ending it, skipping the end silently discarded the whole
        // table under TableOptions, where the table writer buffers everything and emits on end.
        // Both formatter configurations must agree, and neither may lose the table.
        foreach (var buffered in new[] { true, false })
        {
            var options = new MarkoutWriterOptions { IncludeSections = ["includedsection"] };
            if (buffered)
                options.TableOptions = new MarkdownTable.Formatting.TableFormatterOptions();

            var sw = new StringWriter();
            var writer = new MarkoutWriter(sw, new MarkdownFormatter(), options);

            writer.WriteSectionStart(1, "IncludedSection");
            writer.WriteTableStart(["X", "Y"]);
            writer.WriteTableRow(["x", "y"]);
            writer.WriteSectionStart(2, "ExcludedSection");
            writer.WriteTableEnd();
            writer.Flush();

            Assert.Contains("| X | Y |", sw.ToString(), StringComparison.Ordinal);
            Assert.Contains("| x | y |", sw.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Streaming_ATableWhoseStartFailed_WritesNothingForItsRowsOrEnd()
    {
        // A start that throws must leave no half-open table behind. Keeping the table writer meant
        // the rows and the end that followed wrote into a table that never had a header, emitting
        // fragments of a table the formatter had already refused. The caller is at fault for
        // continuing after a throw, but the bytes are the writer's to not produce.
        var writer = MarkoutWriter.Create(new ThrowingStartFormatter());

        Assert.Throws<InvalidOperationException>(() => writer.WriteTableStart(["A"]));
        writer.WriteTableRow(["a"]);
        writer.WriteTableEnd();

        Assert.Equal("", writer.ToString());
    }

    private sealed class NameSharingCulture(string name, CultureInfo inner) : CultureInfo(inner.Name)
    {
        public override string Name { get; } = name;

        public override CompareInfo CompareInfo { get; } = inner.CompareInfo;
    }

    [Fact]
    public void WriteTableStart_RejectsHeaderNamesThatDoNotMatchZeroHeaders()
    {
        // The zero-column early return has to sit behind the arity check: returning first accepted
        // a malformed call in silence on the streaming path while the buffered path rejected the
        // same arguments.
        var writer = MarkoutWriter.Create(new MarkdownFormatter());

        var ex = Assert.Throws<ArgumentException>(() => writer.WriteTableStart([], ["a"]));
        Assert.Contains("same length as headers", ex.Message, StringComparison.Ordinal);
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
    public void Projection_RetargetedMidDocument_StillReportsTheEarlierUnmatchedIncludeList()
    {
        // MarkoutWriterOptions.Projection is a mutable object. Reading it back at finalization let
        // a later exclude projection answer for an include projection that had already cost the
        // caller a table, turning a typo into success-shaped empty output.
        var projection = new MarkoutProjection { IncludeColumns = ["Typo"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });
        writer.WriteTable(["Name"], [["value"]]);

        projection.IncludeColumns = null;
        projection.ExcludeColumns = ["Name"];

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_ALaterMatchingAllowList_DoesNotExcuseAnEarlierUnmatchedOne()
    {
        // Reach used one pair of document-wide counters, so any table matching anything answered
        // for every allow list that had matched nothing. A typo'd selection followed by a working
        // one therefore finalized as success-shaped empty output, silently dropping the table the
        // typo was aimed at. Each allow list has to answer only for itself.
        var projection = new MarkoutProjection { IncludeColumns = ["Typo"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["A"], [["one"]]);
        projection.IncludeColumns = ["B"];
        writer.WriteTable(["B"], [["two"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_RequestedNamesAreSnapshot_NotReadBackFromTheCallersList()
    {
        // The recorded allow list must be a copy: MarkoutProjection.IncludeColumns is a mutable
        // list a caller can edit in place, and holding the reference would let a later edit rewrite
        // the diagnosis of an earlier failure.
        var names = new List<string> { "Typo" };
        var options = new MarkoutWriterOptions { Projection = new MarkoutProjection { IncludeColumns = names } };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["A"], [["one"]]);
        names[0] = "A";

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_AnEarlierExcludeMiss_DoesNotExcuseALaterIncludeTypo()
    {
        // Recording the first miss of any kind latched the exclude exemption: an exclude that
        // legitimately emptied table A then answered for an include typo that missed table B,
        // and the typo finalized as success-shaped empty output. Only an include miss is
        // diagnosable, so an include miss is what has to be recorded.
        var projection = new MarkoutProjection { ExcludeColumns = ["A"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["A"], [["1"]]);
        projection.ExcludeColumns = null;
        projection.IncludeColumns = ["Typo"];
        writer.WriteTable(["B"], [["2"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_ATypoIsStillDiagnosedAcrossANestedStart()
    {
        // A nested start supersedes the open table, and the selection offered to it must survive
        // that: both tables were offered this list and neither matched, so the document is still
        // holding an unsatisfied selection when it finishes.
        var projection = new MarkoutProjection { IncludeColumns = ["Typo"] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTableStart(["A"]);
        writer.WriteTableRow(["first"]);
        writer.WriteTableStart(["B"]);
        writer.WriteTableRow(["second"]);
        writer.WriteTableEnd();

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_OneAllowListSpanningAHeterogeneousDocument_DoesNotThrow()
    {
        // The negative of the case above: one allow list offered to several tables is a selection,
        // not a typo, and a table that does not carry the column renders nothing rather than
        // failing the document.
        var options = new MarkoutWriterOptions { Projection = new MarkoutProjection { IncludeColumns = ["Name"] } };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["Other"], [["one"]]);
        writer.WriteTable(["Name"], [["two"]]);

        Assert.Contains("| Name |", writer.ToString(), StringComparison.Ordinal);
    }

// from ProjectionTests.cs

    [Fact]
    public void Projection_AListThatMatchedATableTheCallerNeverEnded_IsStillCredited()
    {
        // Credit is settled where the matcher answers, not where the bytes land. This document
        // leaves the table open -- supported usage, and what the SectionOrder fuzzer generates --
        // and its header is plainly in the output, so diagnosing "A" as a column the document does
        // not have would contradict the document the caller is holding.
        var sw = new StringWriter();
        var writer = new MarkoutWriter<MarkdownFormatter>(
            sw,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { Projection = MarkoutProjection.WithColumns("A") });

        writer.WriteTableStart(["A"]);
        writer.WriteTableRow(["a"]);
        writer.Flush();

        Assert.Contains("| A |", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_AListOfferedOnlyToTablesItMissed_IsStillReported()
    {
        // The mirror of the test above: the list rendered nothing anywhere it was offered, so it
        // is reported. A table written with the projection detached was never offered the list and
        // so cannot excuse it -- the caller said not to project that table.
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithColumns("Ghost"),
            TableMode = MarkoutTableMode.Jsonl,
        };
        var writer = MarkoutWriter.Create(new TableFormatter(), options);

        writer.WriteTable(["Other"], [["x"]]);
        options.Projection = null;
        writer.WriteTable(new MarkoutTable(["Ghost"], []));
        options.Projection = MarkoutProjection.WithColumns("Ghost");

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Ghost", ex.Message, StringComparison.Ordinal);
    }




    // A colliding pair cannot be written down: HashCode is seeded per process, so the pair that
    // collides today does not collide on the next run. Each axis of in-bucket identity therefore
    // finds its own pair now, by filling a table with one family's digests and probing the other
    // family until it lands in an occupied bucket. Expected probes are 2^32/40000, a few hundred
    // thousand; the bound below is far beyond any plausible run.
    private static (StringComparison Comparison, string[] Names) FindColliding(
        Func<int, (StringComparison Comparison, string[] Names)> family,
        Func<int, (StringComparison Comparison, string[] Names)> probeFamily,
        out (StringComparison Comparison, string[] Names) collidesWith)
    {
        var seen = new Dictionary<int, (StringComparison Comparison, string[] Names)>();
        for (int i = 0; i < 40_000; i++)
        {
            var candidate = family(i);
            seen[MarkoutWriter.SelectionDigest(candidate.Names)] = candidate;
        }

        for (int i = 0; i < 50_000_000; i++)
        {
            var candidate = probeFamily(i);
            if (seen.TryGetValue(MarkoutWriter.SelectionDigest(candidate.Names), out var hit))
            {
                collidesWith = hit;
                return candidate;
            }
        }

        throw new InvalidOperationException("No digest collision found; the search bound is wrong.");
    }

    // Offers the matching selection first so its credit is the thing a confused identity would hand
    // to the second, then offers the second to a table it names nothing in. Separated correctly, the
    // second is unsatisfied and reported; confused, its table vanishes with no diagnostic at all.
    private static void AssertSeparated(
        (StringComparison Comparison, string[] Names) matching,
        (StringComparison Comparison, string[] Names) missing)
    {
        Assert.Equal(
            MarkoutWriter.SelectionDigest(matching.Names),
            MarkoutWriter.SelectionDigest(missing.Names));

        var projection = new MarkoutProjection
        {
            Comparison = matching.Comparison,
            IncludeColumns = matching.Names,
        };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(matching.Names, [[.. matching.Names.Select(_ => "matched")]]);
        projection.Comparison = missing.Comparison;
        projection.IncludeColumns = missing.Names;
        writer.WriteTable(["Unrelated"], [["silently lost"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Equal($"No columns matched projection: {string.Join(", ", missing.Names)}", ex.Message);
    }

    [Fact]
    public void Projection_ACollidingPairOfDifferentLengths_IsStillSeparated()
    {
        // Asking for one column and asking for two are different requests even when their digests
        // agree, so in-bucket identity must compare length rather than assume the digest did.
        var missing = FindColliding(
            i => (StringComparison.Ordinal, [$"C{i}"]),
            i => (StringComparison.Ordinal, ["Shared", $"D{i}"]),
            out var matching);

        Assert.NotEqual(matching.Names.Length, missing.Names.Length);
        AssertSeparated(matching, missing);
    }

    [Fact]
    public void Projection_ACollidingPairDifferingOnlyAfterTheFirstName_IsStillSeparated()
    {
        // In-bucket identity compares the whole name sequence. A pair that collides while agreeing
        // on its first name is the case a check that stopped early would get wrong, and it is
        // reachable only through a genuine collision.
        var missing = FindColliding(
            i => (StringComparison.Ordinal, ["Shared", $"C{i}"]),
            i => (StringComparison.Ordinal, ["Shared", $"D{i}"]),
            out var matching);

        Assert.Equal(matching.Names[0], missing.Names[0]);
        Assert.NotEqual(matching.Names[1], missing.Names[1]);
        AssertSeparated(matching, missing);
    }

    [Fact]
    public void Projection_ManyDistinctSelections_DoNotTurnLookupIntoAScan()
    {
        // The digest exists to keep lookup off a scan of every selection already seen. A digest that
        // stopped distributing would leave every verdict correct and every gate green while turning
        // a document that retargets per table quadratic, so the cost is asserted directly.
        const int Offers = 5_000;
        var projection = new MarkoutProjection { IncludeColumns = ["S0"] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        for (int i = 0; i < Offers; i++)
        {
            projection.IncludeColumns = [$"S{i}"];
            writer.WriteTable([$"S{i}"], [["v"]]);
        }

        Assert.Equal(Offers, writer.ProjectionSelectionCount);
        Assert.InRange(writer.ProjectionSelectionProbeCount, 0, Offers * 4L);
    }


    [Fact]
    public void Table_StreamingHeadersContainingTableSyntax_AreEscaped()
    {
        // Runtime columns are data, and MarkoutTable exists to carry data as columns. A header that
        // contains a pipe or a newline would otherwise close the cell and open another, letting a
        // value forge the table it is printed in. The buffered path escapes; this is the streaming
        // path, which is public API in its own right and was covered by nothing.
        var writer = MarkoutWriter.Create(new MarkdownFormatter());

        writer.WriteTableStart(["A|B", "C\nD"]);
        writer.WriteTableRow(["1", "2"]);
        writer.WriteTableEnd();

        var output = writer.ToString();
        Assert.Contains("| A&#124;B | C D |", output, StringComparison.Ordinal);
        Assert.DoesNotContain("A|B", output, StringComparison.Ordinal);
        Assert.Equal(3, output.Split('\n').Length);
    }

    [Fact]
    public void Projection_AgainstAStreamTarget_IsReportedByFlush()
    {
        // A caller writing to a stream never calls ToString, so Flush is the only place the
        // document-scoped diagnostic can reach them. Losing it there would make the check
        // StringWriter-only, which is silent for exactly the callers who cannot see the output.
        var target = new StringWriter();
        var options = new MarkoutWriterOptions { Projection = new MarkoutProjection { IncludeColumns = ["Typo"] } };
        var writer = MarkoutWriter.Create(target, new MarkdownFormatter(), options);

        writer.WriteTable(["A"], [["v"]]);

        var ex = Assert.Throws<InvalidOperationException>(writer.Flush);
        Assert.Equal("No columns matched projection: Typo", ex.Message);
    }

    [Fact]
    public void Projection_WhenUnsatisfied_IsReportedBeforeAnyOrderedSectionIsEmitted()
    {
        // The check runs before ordered sections are emitted, so a document the caller is about to
        // be told is broken does not first deposit half of itself on the target. Ordering is the
        // whole property here: both orders throw, and only one of them leaves the target clean.
        var target = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            SectionOrder = ["second", "first"],
            Projection = new MarkoutProjection { IncludeColumns = ["Typo"] },
        };
        var writer = MarkoutWriter.Create(target, new MarkdownFormatter(), options);

        writer.WriteHeading(2, "first");
        writer.WriteTable(["A"], [["v"]]);
        writer.WriteHeading(2, "second");

        Assert.Throws<InvalidOperationException>(writer.Flush);
        Assert.Equal("", target.ToString());
    }

    [Fact]
    public void Projection_WhenUnsatisfied_IsReportedBeforeToStringEmitsAnyOrderedSection()
    {
        // ToString finishes a document exactly as Flush does, and holds the same ordering. Gating
        // Flush alone leaves this path free to deposit the document into the target and only then
        // announce it is broken -- after which the caller who retries sees it twice.
        var target = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            SectionOrder = ["second", "first"],
            Projection = new MarkoutProjection { IncludeColumns = ["Typo"] },
        };
        var writer = MarkoutWriter.Create(target, new MarkdownFormatter(), options);

        writer.WriteHeading(2, "first");
        writer.WriteTable(["A"], [["v"]]);
        writer.WriteHeading(2, "second");

        Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Equal("", target.ToString());
    }

    [Fact]
    public void Projection_UnderTheDefaultComparison_StillTellsCaseDifferingNamesApart()
    {
        // The identity of a request is ordinal whatever comparison it is matched under, and the
        // default -- OrdinalIgnoreCase -- is the one nearly every caller gets without asking. The
        // matcher's case-insensitivity is not the request's: "Xyz" asked about a table that has no
        // such column and went unanswered, and folding it together with "XYZ" would let the table
        // that does have the column answer a question nobody asked.
        var projection = new MarkoutProjection { IncludeColumns = ["Xyz"] };
        Assert.Equal(StringComparison.OrdinalIgnoreCase, projection.Comparison);
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["Other"], [["silently lost"]]);
        projection.IncludeColumns = ["XYZ"];
        writer.WriteTable(["XYZ"], [["rendered"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Equal("No columns matched projection: Xyz", ex.Message);
    }

    [Fact]
    public void Table_HeaderNamesOfADifferentLength_AreRejectedAtConstruction()
    {
        // The arity guard is what keeps display headers and structured keys in correspondence. A
        // zero-header table is the case that reaches nothing downstream -- serialization skips it --
        // so construction is the only place the mismatch can still be reported.
        var tooMany = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["A"], ["a", "b"], []));
        Assert.Equal("headerNames", tooMany.ParamName);

        var againstNoHeaders = Assert.Throws<ArgumentException>(
            () => new MarkoutTable([], ["a"], []));
        Assert.Equal("headerNames", againstNoHeaders.ParamName);
    }

    [Fact]
    public void Projection_AnEmptyAllowList_IsRejectedEvenForAZeroColumnTable()
    {
        // Whether the allow list can ever select anything is a property of the projection, not of
        // the table in hand. Both entry points return early for a zero-column table, and checking
        // after that return made an empty list silent on precisely the tables that record no
        // selection for finalization to report later.
        var options = new MarkoutWriterOptions { Projection = new MarkoutProjection { IncludeColumns = [] } };

        var buffered = MarkoutWriter.Create(new MarkdownFormatter(), options);
        var bufferedEx = Assert.Throws<InvalidOperationException>(() => buffered.WriteTable([], []));
        Assert.Contains("IncludeColumns is empty", bufferedEx.Message, StringComparison.Ordinal);

        var streaming = MarkoutWriter.Create(new MarkdownFormatter(), options);
        var streamingEx = Assert.Throws<InvalidOperationException>(() => streaming.WriteTableStart([]));
        Assert.Contains("IncludeColumns is empty", streamingEx.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_TwoSelectionsSharingADigestBucket_AreStillSeparated()
    {
        // The digest only chooses a bucket; what makes two selections the same is the equality check
        // inside it. Without that check a colliding pair shares one record, and the second selection
        // is credited with the first's match -- the table it named nothing in vanishes silently,
        // which is the same silent data loss reference keying produced. HashCode is seeded per
        // process, so the colliding pair has to be found now rather than written down.
        string? first = null;
        string? second = null;
        var seen = new Dictionary<int, string>();
        for (int i = 0; i < 400_000 && second is null; i++)
        {
            var candidate = $"C{i}";
            var digest = MarkoutWriter.SelectionDigest(new[] { candidate });
            if (seen.TryGetValue(digest, out var existing))
            {
                first = existing;
                second = candidate;
            }
            else
            {
                seen[digest] = candidate;
            }
        }

        Assert.NotNull(second);
        Assert.Equal(
            MarkoutWriter.SelectionDigest(new[] { first! }),
            MarkoutWriter.SelectionDigest(new[] { second! }));

        var projection = new MarkoutProjection { IncludeColumns = [first!] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable([first!], [["matched"]]);
        projection.IncludeColumns = [second!];
        writer.WriteTable(["Unrelated"], [["silently lost"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains(second!, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_TwoUnsatisfiedSelections_ReportTheFirstOneMade()
    {
        // Offer order is what makes the diagnostic reproducible: a caller whose document holds two
        // unsatisfied selections is told about the one they made first, not whichever the bucketing
        // happened to surface.
        var projection = new MarkoutProjection { IncludeColumns = ["First"] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["X"], [["a"]]);
        projection.IncludeColumns = ["Second"];
        writer.WriteTable(["Y"], [["b"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Equal("No columns matched projection: First", ex.Message);
    }

    [Fact]
    public void Projection_AListMutatedAfterItMatched_IsANewSelection()
    {
        // IncludeColumns is publicly mutable, so the same list object can carry two different
        // requests. Crediting the second with the first's answer let the second table vanish from
        // the output with no diagnostic at all -- the caller asked for "Typo", got a document
        // containing only the "A" table, and was told nothing.
        var names = new List<string> { "A" };
        var options = new MarkoutWriterOptions { Projection = new MarkoutProjection { IncludeColumns = names } };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["A"], [["rendered"]]);
        names[0] = "Typo";
        writer.WriteTable(["B"], [["silently lost"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_TheSameNamesUnderADifferentComparison_AreADistinctSelection()
    {
        // Comparison is mutable too, and it is half of what a selection means: the same names
        // matched ordinally and matched case-insensitively are two different questions. Sharing
        // one entry between them let the ordinal miss be excused by the insensitive match.
        var projection = new MarkoutProjection
        {
            Comparison = StringComparison.Ordinal,
            IncludeColumns = new List<string> { "Name" },
        };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["name"], [["silently lost"]]);
        projection.Comparison = StringComparison.OrdinalIgnoreCase;
        writer.WriteTable(["name"], [["rendered"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Name", ex.Message, StringComparison.Ordinal);
    }


    [Fact]
    public void Projection_CaseDifferingNames_AreDistinctSelectionsUnderOrdinalMatching()
    {
        // The names half of a selection is compared ordinally, because that is identity of the
        // request, not a second model of the matcher. Under ordinal matching "name" and "Name" ask
        // different questions, and folding them together would let the answerable one excuse the
        // typo.
        var projection = new MarkoutProjection
        {
            Comparison = StringComparison.Ordinal,
            IncludeColumns = new List<string> { "NAME" },
        };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["Name"], [["silently lost"]]);
        projection.IncludeColumns = ["Name"];
        writer.WriteTable(["Name"], [["rendered"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: NAME", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_AnEarlierUnmatchedSelection_SurvivesANestedStartThatRetargets()
    {
        // A nested start replaces the open table, and must not take the document's record of what
        // has already been offered with it. The first selection matched nothing and the second
        // matches, so a start that discarded the offer history would finish clean while the first
        // table rendered nothing.
        var options = new MarkoutWriterOptions { Projection = new MarkoutProjection { IncludeColumns = ["Typo"] } };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTableStart(["A"]);
        writer.WriteTableRow(["first"]);

        options.Projection = new MarkoutProjection { IncludeColumns = ["B"] };
        writer.WriteTableStart(["B"]);
        writer.WriteTableRow(["second"]);
        writer.WriteTableEnd();

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Typo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_ManyOffersOfTheSameSelection_HoldOneEntry()
    {
        // Selections are held by what they ask, not by the object that carried the request, so a
        // document that rebuilds an equivalent projection per table does not accumulate an entry
        // per table. This is a memory bound on caller-driven retargeting, not a micro-optimisation.
        var options = new MarkoutWriterOptions();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        for (int i = 0; i < 5_000; i++)
        {
            options.Projection = MarkoutProjection.WithColumns("Name");
            writer.WriteTable(["Name"], [["v"]]);
        }

        Assert.Equal(1, writer.ProjectionSelectionCount);
    }

    [Fact]
    public void Projection_TwoAliasesOfOneColumn_ProjectItOnce()
    {
        // A column answers to its display header and its stable name, so naming both would project
        // one column twice -- past MarkoutTable's construction-time key check, straight to a JSONL
        // object with a repeated key.
        var options = new MarkoutWriterOptions
        {
            TableMode = MarkoutTableMode.Jsonl,
            Projection = new MarkoutProjection { IncludeColumns = ["Display", "Name"] }
        };
        var writer = MarkoutWriter.Create(new TableFormatter(), options);

        writer.WriteTable(new MarkoutTable(["Display"], ["Name"], [["one"]]));

        Assert.Equal("{\"name\":\"one\"}", writer.ToString());
    }

    [Fact]
    public void Projection_AGlobOverlappingAnExplicitName_ProjectsEachColumnOnce()
    {
        var options = new MarkoutWriterOptions
        {
            TableMode = MarkoutTableMode.Jsonl,
            Projection = new MarkoutProjection { IncludeColumns = ["N*", "Name"] }
        };
        var writer = MarkoutWriter.Create(new TableFormatter(), options);

        writer.WriteTable(new MarkoutTable(["Name", "Note"], [["one", "two"]]));

        Assert.Equal("{\"name\":\"one\",\"note\":\"two\"}", writer.ToString());
    }

    [Theory]
    [InlineData("A\nB", "A B", "A B")]
    [InlineData("A\tB", "A B", "A B")]
    // The tabular formats strip inline Markdown before collapsing whitespace, so a header carrying
    // a code span emits the same text as the bare word. Validating only the whitespace half of the
    // emitters' pipeline let this pair through to duplicate TSV and JSONL keys.
    [InlineData("<code>A</code>", "A", "A")]
    [InlineData("<code>A B</code>", "A\nB", "A B")]
    public void MarkoutTable_RejectsDisplayHeadersThatNormalizeToTheSameEmittedHeader(
        string first, string second, string emitted)
    {
        // Every table format collapses newlines and tabs in a header to spaces, so headers that
        // differ only there are distinct at construction and identical in the output: two columns
        // under one visible heading, and one duplicate key under DisplayName header style.
        var ex = Assert.Throws<ArgumentException>(
            () => new MarkoutTable([first, second], ["n1", "n2"], [["one", "two"]]));

        Assert.Contains($"share the display header '{emitted}'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkoutTable_DisplayHeadersThatDifferOnlyInMarkdown_AreRejectedBeforeTheyCanCollideInTsv()
    {
        // The negative half: proving the pair really would have collided. Without the rejection this
        // table emits two "A" columns in TSV and a JSONL object with one "a" key.
        var ex = Assert.Throws<ArgumentException>(
            () => new MarkoutTable(["<code>A</code>", "A"], [["one", "two"]]));

        Assert.Contains("share the display header 'A'", ex.Message, StringComparison.Ordinal);

        // A header whose inline rendering stays distinct is untouched.
        var writer = MarkoutWriter.Create(new TableFormatter());
        writer.WriteTable(new MarkoutTable(["<code>A</code>", "B"], [["one", "two"]]));
        var lines = writer.ToString().Split('\n');
        Assert.Equal(["A", "B"], lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void WriteTable_RendersNothingForATableWithNoColumns()
    {
        // The generator skips an empty table, but every buffered imperative overload reached the
        // formatter directly and emitted a "|"/"|" husk that is not a table. Guarding only the
        // MarkoutTable overload left the plain one still doing it, so both are pinned here.
        var fromTable = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions());
        fromTable.WriteTable(new MarkoutTable([], []));
        Assert.Equal("", fromTable.ToString());

        var fromHeaders = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions());
        fromHeaders.WriteTable([], []);
        Assert.Equal("", fromHeaders.ToString());
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

        // Anchored to a literal, not only to the hand-assembled document below. Both sides of that
        // comparison route through the same WriteTable(MarkoutTable) implementation, so equality
        // alone survives that method becoming a no-op -- the two sides simply go empty together.
        // A literal is the independent oracle that makes the equality mean something.
        Assert.Equal(
            """
            ## Metadata: #Strings

            | Offset | Value |
            | ------ | ----- |
            | 0x1 | System |
            | 0x8 | Object |
            | 0x10 | String |

            ## Metadata: #Blob

            | Address | Length |
            | ------- | ------ |
            | 0x0 | 12 |
            | 0xC | 4 |
            """,
            viaModel.Replace("\r\n", "\n").TrimEnd('\n'));

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

/// <summary>
/// A streaming formatter whose BeginTable throws, so a table can be started and produce nothing at
/// all -- not even the header an unbuffered start normally emits immediately.
/// </summary>
internal sealed class ThrowingStartFormatter : IMarkoutFormatter, IStreamingTableFormatter
{
    public void BeginTable(TextWriter writer, ReadOnlySpan<string> headers, MarkoutWriterOptions options)
        => throw new InvalidOperationException("begin formatter failed");

    public void WriteRow(TextWriter writer, ReadOnlySpan<string> values) => writer.Write("[ROW]");

    public void EndTable(TextWriter writer, int skippedRows) => writer.Write("[END]");
}
