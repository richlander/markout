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
    public void Projection_CaseDifferingNames_AreOneSelectionUnderCaseInsensitiveMatching()
    {
        // Reach identity compared ordinally while matching defaults to OrdinalIgnoreCase, so
        // "NAME" and "name" became two entries for what the matcher treats as one selection. The
        // second could never be offered anything the first had not already matched, so a document
        // that projected correctly threw anyway.
        var projection = new MarkoutProjection { IncludeColumns = ["NAME"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Other"], [["x"]]);
        projection.IncludeColumns = ["name"];
        writer.WriteTable(["Name"], [["kept"]]);

        Assert.Contains("kept", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_CaseDifferingNames_AreDistinctSelectionsUnderOrdinalMatching()
    {
        // The negative of the case above: under an ordinal comparison the matcher does tell those
        // two lists apart, so merging them would let one excuse the other's miss.
        var projection = new MarkoutProjection { Comparison = StringComparison.Ordinal, IncludeColumns = ["NAME"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Other"], [["x"]]);
        projection.IncludeColumns = ["name"];
        writer.WriteTable(["name"], [["kept"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: NAME", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_TheSameNamesUnderADifferentComparison_AreADistinctSelection()
    {
        // Identity has to carry the comparison as well as the names. Ignoring it let a match found
        // case-insensitively answer for a miss that happened under ordinal matching.
        var projection = new MarkoutProjection { Comparison = StringComparison.Ordinal, IncludeColumns = ["Name"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["name"], [["x"]]);
        projection.Comparison = StringComparison.OrdinalIgnoreCase;
        writer.WriteTable(["name"], [["kept"]]);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("No columns matched projection: Name", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_NamesEqualUnderTheComparisonButNotUnderCaseFolding_AreOneSelection()
    {
        // Reach buckets must be keyed on a hash consistent with the comparison. Folding case by
        // hand was not: composed "\u00e9" and decomposed "e\u0301" are equal under
        // InvariantCultureIgnoreCase -- the matcher resolves either against the other's column --
        // but they fold to different strings, so one selection split into two entries and the half
        // that was never offered a matching table threw.
        var projection = new MarkoutProjection
        {
            Comparison = StringComparison.InvariantCultureIgnoreCase,
            IncludeColumns = ["\u00e9"]
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Other"], [["x"]]);
        projection.IncludeColumns = ["e\u0301"];
        writer.WriteTable(["\u00e9"], [["kept"]]);

        Assert.Contains("kept", writer.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    // The matcher resolves a display name and its snake_case alias to the same column.
    [InlineData("My Column", "my_column", "My Column")]
    // "A*" and "A**" are the same glob to the matcher.
    [InlineData("A**", "A*", "Alpha")]
    public void Projection_ListsTheMatcherCannotTellApart_DoNotThrowWhenEitherMatches(
        string first, string second, string header)
    {
        // Reach used to decide this by canonicalizing the requested text, which was a second and
        // weaker model of the matcher: it knew about case and duplicates but not about snake_case
        // aliases or glob spelling, so a list the matcher would happily have resolved was reported
        // as an unmatched typo. Reach now asks the matcher instead.
        var projection = new MarkoutProjection { IncludeColumns = [first] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Other"], [["x"]]);
        projection.IncludeColumns = [second];
        writer.WriteTable([header], [["kept"]]);

        Assert.Contains("kept", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_NamesEqualUnderTheDefaultComparison_AreNotSplitByCaseFolding()
    {
        // Greek final sigma: "\u03c3" and "\u03c2" share an invariant uppercase and so are equal
        // under the default OrdinalIgnoreCase matching, but they lowercase differently. Any reach
        // scheme that folds case by hand splits them and throws for a document that projected fine.
        var projection = new MarkoutProjection { IncludeColumns = ["\u03c3"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["\u03c3"], [["kept"]]);
        projection.IncludeColumns = ["\u03c2"];
        writer.WriteTable(["Other"], [["x"]]);

        Assert.Contains("kept", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_FinalizedUnderADifferentCulture_StillJudgesByTheCultureItMatchedIn()
    {
        // Reach records the comparison but the comparison is only half the question: a
        // CurrentCulture match means whatever the culture said when the list was offered, and
        // finalization can run under a different one. Turkish is the classic separator -- "I" and
        // dotless "i" are the same letter case-insensitively in tr-TR and different in en-US.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var projection = new MarkoutProjection
            {
                Comparison = StringComparison.CurrentCultureIgnoreCase,
                IncludeColumns = ["I"]
            };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

            writer.WriteTable(["Other"], [["x"]]);
            projection.IncludeColumns = ["\u0131"];
            writer.WriteTable(["\u0131"], [["kept"]]);

            // Judging "I" under en-US finds nothing and reports a typo for a document that rendered.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Contains("kept", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Projection_FinalizedUnderADifferentCulture_StillReportsAListThatMatchedNothing()
    {
        // The fail-open direction, which matters more: a list that genuinely matched nothing under
        // the culture it was offered in must not be excused by a culture that would have matched.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var projection = new MarkoutProjection
            {
                Comparison = StringComparison.CurrentCultureIgnoreCase,
                IncludeColumns = ["I"]
            };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

            writer.WriteTable(["\u0131"], [["x"]]);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
            Assert.Contains("No columns matched projection", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Projection_OfferedUnderTwoCultures_IsExcusedByAMatchInEither()
    {
        // Culture belongs to the probe, not to identity. Splitting the entry per culture makes
        // "matched nothing anywhere it was offered" mean "anywhere under this one culture", so a
        // list busy selecting a column in one culture is reported as an unmatched typo because a
        // later offer under a second culture happened to miss.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var projection = new MarkoutProjection
            {
                Comparison = StringComparison.CurrentCultureIgnoreCase,
                IncludeColumns = ["I"]
            };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

            writer.WriteTable(["\u0131"], [["matched"]]);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            projection.IncludeColumns = ["I"];
            writer.WriteTable(["Other"], [["x"]]);

            Assert.Contains("matched", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Projection_OfferedUnderTwoCultures_StillReportsAListThatMatchedInNeither()
    {
        // The other side of the same coin: merging the cultures must not become an excuse.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var projection = new MarkoutProjection
            {
                Comparison = StringComparison.CurrentCultureIgnoreCase,
                IncludeColumns = ["Typo"]
            };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

            writer.WriteTable(["A"], [["x"]]);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            projection.IncludeColumns = ["Typo"];
            writer.WriteTable(["B"], [["y"]]);

            var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
            Assert.Contains("Typo", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Projection_AProbeThatSwapsCulture_RestoresIt()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var projection = new MarkoutProjection
            {
                Comparison = StringComparison.CurrentCultureIgnoreCase,
                IncludeColumns = ["Typo"]
            };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });
            writer.WriteTable(["A"], [["x"]]);

            Assert.Throws<InvalidOperationException>(() => writer.ToString());
            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
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
    public void Projection_ColumnsSharingADisplayHeaderButNotAStableName_AreBothInTheUniverse()
    {
        // The universe is keyed on the (display header, stable name) PAIR, not the display header
        // alone. Two tables can show the same human-facing header while carrying different stable
        // names, and a list may name either. Collapsing the key to the display header keeps only
        // the first pair, so a list naming the second stable name is told the document never had
        // that column -- a wrong diagnostic about a column that is plainly rendered.
        //
        // Reaching the universe at all requires the list to miss every table it is OFFERED: a list
        // that matches even once is excused per table and never probes. So both same-display
        // tables are written with the projection detached, and the only table the list sees has
        // nothing to do with them.
        var projection = new MarkoutProjection { IncludeColumns = ["size_pages"] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        options.Projection = null;
        writer.WriteTable(new MarkoutTable(["Size"], ["size_bytes"], [["10"]]));
        writer.WriteTable(new MarkoutTable(["Size"], ["size_pages"], [["2"]]));
        options.Projection = projection;
        writer.WriteTable(["Unrelated"], [["u"]]);

        writer.Flush();
    }

    [Fact]
    public void Projection_ColumnsOfAStreamingTableNeverEnded_DoNotExcuseATypo()
    {
        // The streaming sibling of the aborted buffered table, and the reason "the header was
        // already written" is not a safe answer: with TableOptions set, the Markdown table writer
        // buffers the entire table and emits NOTHING until WriteTableEnd. A table that is started
        // and then abandoned therefore contributes zero bytes, so its columns must not join the
        // universe and excuse a typo. Recording is staged at start and committed at end.
        var projection = new MarkoutProjection { IncludeColumns = ["B"] };
        var options = new MarkoutWriterOptions
        {
            Projection = projection,
            TableOptions = new MarkdownTable.Formatting.TableFormatterOptions(),
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["A"], [["a"]]);
        options.Projection = null;
        writer.WriteTableStart(["B"]);
        options.Projection = projection;

        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("B", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_ColumnsOfAStreamingTableThatEnded_DoCount()
    {
        // The negative half: a streaming table that completes normally must still put its columns
        // in the universe, or deferring the record to WriteTableEnd would simply lose them.
        var projection = new MarkoutProjection { IncludeColumns = ["B"] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["A"], [["a"]]);
        options.Projection = null;
        writer.WriteTableStart(["B"]);
        writer.WriteTableRow(["b"]);
        writer.WriteTableEnd();
        options.Projection = projection;

        Assert.Contains("| B |", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_ColumnsOfATableThatAbortedMidRender_DoNotExcuseATypo()
    {
        // Rows can be a lazy sequence that throws part-way, which aborts the table: nothing of it
        // reaches the document. Its columns must not join the universe, or a genuine typo that
        // happens to name one of them is excused by a table the reader never sees. Every other
        // defect in this mechanism has been fail-closed -- a spurious throw, annoying and obvious.
        // This one is fail-open and silent, which is the direction a diagnostic must never take.
        var projection = new MarkoutProjection { IncludeColumns = ["Typo"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Valid"], [["v"]]);
        Assert.Throws<InvalidOperationException>(() => writer.WriteTable(["Typo"], ThrowingRows()));

        var ex = Assert.Throws<InvalidOperationException>(writer.Flush);
        Assert.Contains("Typo", ex.Message, StringComparison.Ordinal);

        static IEnumerable<string[]> ThrowingRows()
        {
            yield return ["val"];
            throw new InvalidOperationException("row enumeration failed");
        }
    }

    [Fact]
    public void Projection_ColumnsRenderedWhileTheProjectionWasDetached_StillCount()
    {
        // MarkoutWriterOptions.Projection is publicly mutable, so "was a projection set when this
        // table rendered" is not the same question as "did this document have this column". Only
        // the second one is the universe's business: a caller who detaches the projection around a
        // table must not be told the column that table rendered was a typo.
        var projection = new MarkoutProjection { IncludeColumns = ["B"] };
        var options = new MarkoutWriterOptions { Projection = projection };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(["A"], [["a"]]);   // misses; excused only if "B" is known to exist
        options.Projection = null;            // detached -- this table is rendered whole
        writer.WriteTable(["B"], [["b"]]);
        options.Projection = projection;

        Assert.Contains("| B |", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_FinalizingTwice_DoesNotRepeatTheProbes()
    {
        // Flush() is callable repeatedly and ToString() finalizes too, so an excused list must stay
        // excused rather than be re-proved. The universe only ever grows, so a list the document has
        // already satisfied cannot become unsatisfied -- repeating the probe is pure cost, and it is
        // the expensive path, one matcher run over every distinct column in the document.
        const int Tables = 200;
        var projection = new MarkoutProjection();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        for (int i = 0; i < Tables; i++)
        {
            projection.IncludeColumns = ["C" + (i + 1)];   // misses here, excused by the next table
            writer.WriteTable(["C" + i], [["v"]]);
        }

        projection.IncludeColumns = ["C" + Tables];
        writer.WriteTable(["C" + Tables], [["v"]]);

        writer.Flush();
        var afterFirst = writer.ProjectionResolveCount;
        writer.Flush();
        var afterSecond = writer.ProjectionResolveCount;

        Assert.Equal(afterFirst, afterSecond);
    }

    [Fact]
    public void Projection_AnUnprojectedTablesColumns_AreStillColumnsTheDocumentOffered()
    {
        // The universe answers "did this document have these columns". A table rendered while the
        // allow list was cleared is still a table this document had, so its columns belong in the
        // universe -- otherwise clearing IncludeColumns hides the very column an earlier list was
        // retargeted towards, and a document that rendered it reports it as a typo.
        var projection = new MarkoutProjection();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        projection.IncludeColumns = ["B"];
        writer.WriteTable(["A"], [["a"]]);      // misses; excused only if "B" is known to exist

        projection.IncludeColumns = null;        // cleared -- this table is rendered whole
        writer.WriteTable(["B"], [["b"]]);

        var markdown = writer.ToString();
        Assert.Contains("| B |", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_Finalization_DoesNotPinTheCallersAmbientCulture()
    {
        // Probing under a recorded culture means making it ambient, and assigning CurrentCulture --
        // including to restore it -- replaces inheritance from DefaultThreadCurrentCulture with an
        // explicit override that outlives the call. A document that rendered perfectly well must not
        // leave that behind, so the probe happens on a thread whose ambient state dies with it.
        //
        // The culture must MOVE between recording and finalizing, or no swap happens and the test
        // proves only that the no-swap fast path does nothing -- which is how the first version of
        // this test passed with isolation disabled. It is moved by changing the thread default, so
        // that the caller never acquires an explicit override of its own and the only thing that
        // could create one is the code under test.
        //
        // All three cultures are invariant in behaviour and differ only in Name, so this test
        // cannot change how any concurrently running test formats anything.
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        var originalCurrent = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.DefaultThreadCurrentCulture = new NameSharingCulture("recorded", CultureInfo.InvariantCulture);

            var projection = new MarkoutProjection
            {
                Comparison = StringComparison.CurrentCultureIgnoreCase,
                IncludeColumns = ["B"]
            };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });
            writer.WriteTable(["A"], [["a"]]);   // misses, so finalization must probe under "recorded"
            projection.IncludeColumns = null;
            writer.WriteTable(["B"], [["b"]]);

            // Inherited, not assigned: the caller still has no override of its own.
            CultureInfo.DefaultThreadCurrentCulture = new NameSharingCulture("finalizing", CultureInfo.InvariantCulture);
            Assert.Equal("finalizing", CultureInfo.CurrentCulture.Name);

            _ = writer.ToString();

            // Inheritance must still be live: changing the default has to reach this context.
            CultureInfo.DefaultThreadCurrentCulture = new NameSharingCulture("after", CultureInfo.InvariantCulture);
            Assert.Equal("after", CultureInfo.CurrentCulture.Name);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
            CultureInfo.CurrentCulture = originalCurrent;
        }
    }

    [Fact]
    public void Projection_CulturesSharingANameButNotACompareInfo_AreBothProbed()
    {
        // Recorded cultures are deduplicated on CompareInfo, which is the thing that decides a
        // match. CultureInfo.Name is virtual and does not: these two agree on Name and disagree on
        // comparison, so keying on the name drops the second one's semantics and the list that only
        // it can excuse is reported as a typo.
        var original = CultureInfo.CurrentCulture;
        try
        {
            var turkish = new NameSharingCulture("shared", CultureInfo.GetCultureInfo("tr-TR"));
            var english = new NameSharingCulture("shared", CultureInfo.GetCultureInfo("en-US"));

            var projection = new MarkoutProjection { Comparison = StringComparison.CurrentCultureIgnoreCase };
            var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

            CultureInfo.CurrentCulture = english;
            projection.IncludeColumns = ["Kept"];
            writer.WriteTable(["Kept", "\u0131"], [["x", "y"]]);   // dotless i enters the universe

            projection.IncludeColumns = ["I"];
            writer.WriteTable(["Nope"], [["z"]]);                  // misses under the en flavour

            CultureInfo.CurrentCulture = turkish;
            projection.IncludeColumns = ["I"];
            writer.WriteTable(["Nope"], [["z"]]);                  // misses under the tr flavour too

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Excused: under the Turkish flavour, "I" matches the dotless i the document rendered.
            _ = writer.ToString();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private sealed class NameSharingCulture(string name, CultureInfo inner) : CultureInfo(inner.Name)
    {
        public override string Name { get; } = name;

        public override CompareInfo CompareInfo { get; } = inner.CompareInfo;
    }

    [Fact]
    public void Projection_ManyRetargetedMatchingLists_NeitherScanNorProbe()
    {
        // Reach has silently gone quadratic twice: once when the entry lookup was a linear scan,
        // and once when an unmatched list was re-probed against every table rather than against one
        // deduplicated column universe. A wall-clock bound did not catch either -- loose enough to
        // be stable in CI is looser than the regression. So assert the structure instead.
        //
        // This is the realistic shape: a projection retargeted per table, every list matching.
        const int Tables = 20_000;
        var projection = new MarkoutProjection();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        for (int i = 0; i < Tables; i++)
        {
            projection.IncludeColumns = ["C" + i];
            writer.WriteTable(["C" + i], [["v"]]);
        }

        _ = writer.ToString();

        // Every list matched the table it was offered, so finalization has nothing to ask: the only
        // resolves are the one each rendered table needs. Counted inside MarkoutProjection rather
        // than at the probe site, so a resolve added anywhere -- including one this test never
        // anticipated -- lands here. (The comparison bound below is single-site and cannot make
        // that claim; it proves the entry lookup buckets, and nothing about other work.)
        Assert.Equal(Tables, writer.ProjectionResolveCount);

        // Bucketed lookup compares against collisions only. A linear scan over all entries would be
        // ~200,000,000 comparisons here; the bound is loose enough to tolerate hash collisions and
        // still three orders of magnitude below that.
        Assert.True(
            writer.ProjectionReachEntryComparisons < 4 * Tables,
            $"Entry lookup made {writer.ProjectionReachEntryComparisons} comparisons for {Tables} distinct lists, which means it is scanning rather than bucketing.");

        // Every column here is distinct, so this pins the universe to one entry per column and no
        // more -- it is the no-duplication half. The deduplicating half is asserted separately by
        // Projection_ManyTablesSharingColumns_HoldOneUniverseEntryEach, which is where repeated
        // columns actually occur.
        Assert.Equal(Tables, writer.ProjectionUniverseSize);
    }

    [Fact]
    public void Projection_ManyTablesSharingColumns_HoldOneUniverseEntryEach()
    {
        // The universe holds distinct columns, not columns-per-table. This is the property that
        // bounds a probe to a single matcher run over the document's column set, and so the thing
        // that makes the resolve counter mean what it says: without it a probe is O(tables), and
        // the quadratic this whole design exists to remove is back with every counter still green.
        //
        // What this does NOT prove: that insertion is O(1). That rests on the HashSet in
        // RecordProjectedTable, which no counter here observes -- replacing it with a linear scan
        // over the universe would keep every assertion in this file passing.
        const int Tables = 5_000;
        string[] headers = ["A", "B", "C", "D", "E"];
        var projection = new MarkoutProjection { IncludeColumns = ["A"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        for (int i = 0; i < Tables; i++)
            writer.WriteTable(headers, [["1", "2", "3", "4", "5"]]);

        _ = writer.ToString();

        Assert.Equal(headers.Length, writer.ProjectionUniverseSize);
    }

    [Fact]
    public void Projection_ManyRetargetedMissingLists_AskTheMatcherOncePerList()
    {
        // The other half, and the path the timing test could not reach at all: lists that MISS the
        // table they were offered and are excused by a later one. Re-probing each against every
        // table was quadratic; probing one column universe is one resolve per unmatched list.
        const int Tables = 2_000;
        var projection = new MarkoutProjection();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        for (int i = 0; i < Tables; i++)
        {
            // Names the NEXT table's column, so it misses here and is excused later.
            projection.IncludeColumns = ["C" + (i + 1)];
            writer.WriteTable(["C" + i], [["v"]]);
        }

        // The last list names a column no table ever had, so the document is diagnosably wrong.
        var ex = Assert.Throws<InvalidOperationException>(() => writer.ToString());
        Assert.Contains("C" + Tables, ex.Message, StringComparison.Ordinal);

        // One resolve per rendered table, plus at most one probe per distinct list -- never one per
        // (list, table) pair, which would be 4,000,000 here.
        Assert.True(
            writer.ProjectionResolveCount <= 2 * Tables,
            $"Finalization performed {writer.ProjectionResolveCount} resolves for {Tables} lists and {Tables} tables, which means it is probing per table rather than against the column universe.");
    }

    [Fact]
    public void Projection_ASeparatorInsideAColumnName_CannotForgeAMatch()
    {
        // Bucket collisions are safe because entries are still compared exactly, so a name carrying
        // whatever character the key is built from cannot make two distinct selections share an
        // entry and excuse each other.
        var projection = new MarkoutProjection { IncludeColumns = ["A\u0001B"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Other"], [["x"]]);
        projection.IncludeColumns = ["A", "B"];
        writer.WriteTable(["A", "B"], [["1", "2"]]);

        Assert.Throws<InvalidOperationException>(() => writer.ToString());
    }

    [Fact]
    public void Projection_ARepeatedNameIsTheSameSelectionAsTheNameAlone()
    {
        // A repeated name selects its column once, so ["A", "A"] and ["A"] are indistinguishable to
        // the matcher and must not become two entries.
        var projection = new MarkoutProjection { IncludeColumns = ["A", "A"] };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { Projection = projection });

        writer.WriteTable(["Other"], [["x"]]);
        projection.IncludeColumns = ["A"];
        writer.WriteTable(["A"], [["kept"]]);

        Assert.Contains("kept", writer.ToString(), StringComparison.Ordinal);
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
