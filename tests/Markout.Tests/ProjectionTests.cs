using Markout;
using Markout.Formatting;

namespace Markout.Tests;

public class ProjectionTests
{
    [Fact]
    public void DeferredHeading_DoesNotLeakIntoFollowingHeadlessSection()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteField("dropped", "gone");
        writer.WriteSectionStart(2, "Beta", headless: true);
        writer.WriteParagraph("beta-body");

        Assert.Equal("beta-body", writer.ToString());
    }

    [Fact]
    public void DeferredNestedHeadings_FlushFromOuterToInnerWhenContentSurvives()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(2, "Outer");
        writer.WriteSectionStart(3, "Inner");
        writer.WriteParagraph("body");
        writer.WriteSectionEnd();
        writer.WriteSectionEnd();

        Assert.Equal("## Outer\n\n### Inner\n\nbody", writer.ToString());
    }

    [Fact]
    public void DeferredAncestorHeading_PrecedesAnOrdinaryNestedHeading()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(2, "Outer");
        writer.WriteHeading(3, "Inner");

        Assert.Equal("## Outer\n\n### Inner", writer.ToString());
    }

    [Fact]
    public void DeferredDocumentTitle_RemainsBeforeReorderedSections()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped"),
            SectionOrder = ["Beta", "Alpha"]
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(1, "Title");
        writer.WriteSectionStart(2, "Alpha");
        writer.WriteParagraph("alpha-body");
        writer.WriteSectionStart(2, "Beta");
        writer.WriteParagraph("beta-body");

        var output = writer.ToString();
        Assert.StartsWith("# Title", output, StringComparison.Ordinal);
        Assert.True(
            output.IndexOf("## Beta", StringComparison.Ordinal) <
            output.IndexOf("## Alpha", StringComparison.Ordinal));
    }

    [Fact]
    public void DeferredDocumentTitle_RemainsBeforeSectionWithOpeningBlankLine()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped"),
            SectionOrder = ["Beta", "Alpha"]
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(1, "Title");
        writer.WriteSectionStart(2, "Alpha");
        writer.WriteBlankLine();
        writer.WriteParagraph("alpha-body");
        writer.WriteSectionStart(2, "Beta");
        writer.WriteParagraph("beta-body");

        Assert.StartsWith("# Title", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void DeferredHeading_PrecedesAnExplicitOpeningBlankLine()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        });

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteBlankLine();
        writer.WriteParagraph("body");

        Assert.Equal("## Alpha\n\nbody", writer.ToString());
    }

    [Fact]
    public void ExplicitBlankLine_DoesNotKeepAnOtherwiseEmptyDeferredSection()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        });

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteField("dropped", "gone");
        writer.WriteBlankLine();
        writer.WriteSectionEnd();

        Assert.Equal("", writer.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ExplicitBlankLine_AfterDeferredSectionContentIsNotDiscarded(int level)
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        });

        writer.WriteSectionStart(level, "Section");
        writer.WriteParagraph("one");
        writer.WriteBlankLine();
        writer.WriteSectionEnd();
        writer.WriteParagraph("two");

        Assert.EndsWith("one\n\ntwo", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void DisablingProjection_DiscardsAnEmptyDeferredSiblingHeading()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("dropped")
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteField("dropped", "gone");
        options.Projection = null;
        writer.WriteSectionStart(2, "Beta", headless: true);
        writer.WriteParagraph("beta-body");

        Assert.Equal("beta-body", writer.ToString());
    }

    // --- Column projection: IncludeColumns ---

    [Fact]
    public void IncludeColumns_FiltersTableToSpecifiedColumns()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name", "TFM"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version", "TFM", "Signed");
        orch.WriteTableRow("Foo.dll", "1.0.0", "net8.0", "yes");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("TFM", output);
        Assert.Contains("Foo.dll", output);
        Assert.Contains("net8.0", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("1.0.0", output);
        Assert.DoesNotContain("Signed", output);
        Assert.DoesNotContain("yes", output);
    }

    [Fact]
    public void IncludeColumns_ReordersColumnsToMatchSpecifiedOrder()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["TFM", "Name"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version", "TFM");
        orch.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        // TFM should appear before Name
        int tfmIndex = output.IndexOf("TFM");
        int nameIndex = output.IndexOf("Name");
        Assert.True(tfmIndex < nameIndex, "TFM should appear before Name in output");
    }

    [Fact]
    public void IncludeColumns_CaseInsensitiveByDefault()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["name", "tfm"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version", "TFM");
        orch.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("TFM", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeColumns_MixedUnmatchedColumnsIgnored()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name", "NonExistent"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo.dll", "1.0.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo.dll", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeColumns_EmptyList_ReportsTheEmptyAllowListRatherThanAnUnmatchedName()
    {
        // An empty allow list selects nothing and can never select anything, in this table or any
        // other, so it is a caller error rather than a table that legitimately matched nothing.
        // It is reported where the projection is offered -- there is nothing to learn from the
        // rest of the document that could make an empty list meaningful.
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = [] }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        var ex = Assert.Throws<InvalidOperationException>(() => orch.WriteTableStart("Name", "Version"));
        Assert.Contains("IncludeColumns is empty", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ColumnProjection_RejectsNullNamesWithTheOptionAndIndex(bool include)
    {
        var projection = new MarkoutProjection();
        if (include)
            projection.IncludeColumns = [null!];
        else
            projection.ExcludeColumns = [null!];

        var ex = Assert.Throws<ArgumentException>(
            () => projection.ResolveColumns(["Name"]));

        Assert.Equal(include ? "IncludeColumns" : "ExcludeColumns", ex.ParamName);
        Assert.Contains("index 0", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludeColumns_MutatedToContainNull_IsRejectedWhenSnapshotted()
    {
        var names = new List<string> { "Name" };
        var projection = new MarkoutProjection { IncludeColumns = names };
        names[0] = null!;

        var ex = Assert.Throws<ArgumentException>(
            () => projection.ResolveColumns(["Name"]));

        Assert.Equal("IncludeColumns", ex.ParamName);
        Assert.Contains("index 0", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FieldProjection_RejectsNullNamesWithTheOptionAndIndex(bool include)
    {
        var projection = new MarkoutProjection();
        if (include)
            projection.IncludeFields = [null!];
        else
            projection.ExcludeFields = [null!];
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { Projection = projection });

        var ex = Assert.Throws<ArgumentException>(() => writer.WriteField("Name", "value"));

        Assert.Equal(include ? "IncludeFields" : "ExcludeFields", ex.ParamName);
        Assert.Contains("index 0", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcludeColumns_ExcludingEveryColumn_RendersNothingRatherThanFailing()
    {
        // An exclude projection that empties a table named columns that ARE there and asked for
        // them to go, so nothing is the correct answer to a well-formed request.
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { ExcludeColumns = ["Name", "Version"] }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo.dll", "1.0.0");
        orch.WriteTableEnd();

        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void IncludeColumns_AllUnmatchedColumns_ThrowsWhenNothingInTheDocumentMatched()
    {
        // One table, and the projection reaches none of it: the projection named a column this
        // document does not have. A table that matches nothing while a sibling matches is the
        // case that renders nothing -- see MarkoutTableTests -- but a document that offered the
        // projection something and satisfied none of it fails closed.
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["NonExistent"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo.dll", "1.0.0");
        orch.WriteTableEnd();

        var ex = Assert.Throws<InvalidOperationException>(() => orch.Complete());
        Assert.Contains("No columns matched projection: NonExistent", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryResolveColumns_AllUnmatched_ReturnsNoMatches()
    {
        var projection = MarkoutProjection.WithColumns("NonExistent");

        var success = projection.TryResolveColumns(["Name", "Return Type"], ["Name", "ReturnType"], out var resolution);

        Assert.False(success);
        Assert.Equal(ColumnProjectionResolutionKind.NoMatches, resolution.Kind);
        Assert.Empty(resolution.ColumnMap);
        Assert.Equal(["NonExistent"], resolution.UnmatchedColumns);
    }

    [Fact]
    public void TryResolveColumns_MixedUnmatched_ReturnsMatchedWithUnmatchedColumns()
    {
        var projection = MarkoutProjection.WithColumns("return_type", "NonExistent");

        var success = projection.TryResolveColumns(["Name", "Return Type"], ["Name", "ReturnType"], out var resolution);

        Assert.True(success);
        Assert.Equal(ColumnProjectionResolutionKind.Matched, resolution.Kind);
        Assert.Equal([1], resolution.ColumnMap);
        Assert.Equal(["NonExistent"], resolution.UnmatchedColumns);
    }

    [Fact]
    public void DocumentSchema_ValidateProjection_AcceptsStableAndSnakeCaseNames()
    {
        var schema = new DocumentSchema()
            .Add("Methods", "column",
                new SchemaItem("Name", "column", "Name"),
                new SchemaItem("Return Type", "column", "ReturnType"));

        var validation = schema.ValidateProjection("Methods", ["return_type", "ReturnType"]);

        Assert.True(validation.IsValid);
        Assert.Equal(["return_type", "ReturnType"], validation.Resolved);
        Assert.Empty(validation.Unresolved);
    }

    [Fact]
    public void SchemaItem_EmptyStableNameFallsBackToTheEmittedDisplayKey()
    {
        var item = new SchemaItem("Display Name", "column", "");
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });

        writer.WriteTable(["Display Name"], [""], [["value"]]);

        Assert.Equal("display_name", item.Key);
        Assert.Contains($"\"{item.Key}\"", writer.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(StringComparison.InvariantCultureIgnoreCase)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase)]
    [InlineData(StringComparison.OrdinalIgnoreCase)]
    public void IncludeColumns_GlobHonorsIgnoreCaseComparison(StringComparison comparison)
    {
        var projection = new MarkoutProjection
        {
            Comparison = comparison,
            IncludeColumns = ["a*"]
        };

        var resolution = projection.ResolveColumns(["Alpha"]);

        Assert.Equal(ColumnProjectionResolutionKind.Matched, resolution.Kind);
        Assert.Equal([0], resolution.ColumnMap);
    }

    [Theory]
    [InlineData(StringComparison.InvariantCulture)]
    [InlineData(StringComparison.InvariantCultureIgnoreCase)]
    [InlineData(StringComparison.CurrentCulture)]
    [InlineData(StringComparison.CurrentCultureIgnoreCase)]
    public void IncludeColumns_CultureAwareGlobMatchesCanonicalUnicodeEquivalents(
        StringComparison comparison)
    {
        var projection = new MarkoutProjection
        {
            Comparison = comparison,
            IncludeColumns = ["Caf\u00e9*"]
        };

        var resolution = projection.ResolveColumns(["Cafe\u0301 Value"]);

        Assert.Equal(ColumnProjectionResolutionKind.Matched, resolution.Kind);
        Assert.Equal([0], resolution.ColumnMap);
    }

    // --- Column projection: ExcludeColumns ---

    [Fact]
    public void ExcludeColumns_RemovesSpecifiedColumns()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeColumns = ["Version", "Signed"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version", "TFM", "Signed");
        orch.WriteTableRow("Foo.dll", "1.0.0", "net8.0", "yes");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("TFM", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("Signed", output);
    }

    [Fact]
    public void ExcludeColumns_PreservesOriginalOrder()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeColumns = ["Version"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version", "TFM");
        orch.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        int nameIndex = output.IndexOf("Name");
        int tfmIndex = output.IndexOf("TFM");
        Assert.True(nameIndex < tfmIndex, "Name should appear before TFM (original order)");
    }

    // --- Column projection with WriteTable (batch API) ---

    [Fact]
    public void IncludeColumns_WorksWithBatchWriteTable()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTable(
            ["Name", "Version", "TFM"],
            [["Foo.dll", "1.0.0", "net8.0"], ["Bar.dll", "2.0.0", "net9.0"]]);
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo.dll", output);
        Assert.Contains("Bar.dll", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("TFM", output);
    }

    [Fact]
    public void IncludeColumns_MatchesStableHeaderNames()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["ReturnType"]
            }
        };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Kind", "Return Type", "Detail"],
            ["Kind", "ReturnType", "Detail"],
            [["method", "void", "15"]]);

        var output = sw.ToString();
        Assert.Contains("Return Type", output);
        Assert.Contains("void", output);
        Assert.DoesNotContain("Kind", output);
        Assert.DoesNotContain("Detail", output);
    }

    [Fact]
    public void IncludeColumns_MatchesSnakeCaseStableHeaderNames()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            TableMode = MarkoutTableMode.Tsv,
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["return_type"]
            }
        };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Kind", "Return Type", "Detail"],
            ["Kind", "ReturnType", "Detail"],
            [["method", "void", "15"]]);

        Assert.Equal("return_type\nvoid\n", sw.ToString().ReplaceLineEndings("\n"));
    }

    // --- Column projection with MarkdownFormatter ---

    [Fact]
    public void IncludeColumns_WorksWithMarkdownFormatter()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name", "TFM"]
            }
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("| Name", output);
        Assert.Contains("TFM", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Column projection with TableFormatter ---

    [Fact]
    public void IncludeColumns_WorksWithTableFormatter()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = sw.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo.dll", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("TFM", output);
    }

    // --- Field projection: IncludeFields ---

    [Fact]
    public void IncludeFields_FiltersScalarFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Name", "License"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "9.0.0"),
            new MarkoutField("License", "MIT"));
        var output = orch.ToString();
        Assert.Contains("Name: System.Text.Json", output);
        Assert.Contains("License: MIT", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeFields_FiltersBooleanFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Signed"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Signed", "yes"));
        var output = orch.ToString();
        Assert.Contains("Signed: yes", output);
        Assert.DoesNotContain("Name", output);
    }

    [Fact]
    public void IncludeFields_FiltersGenericFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Count"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Count", "42"));
        var output = orch.ToString();
        Assert.Contains("Count: 42", output);
        Assert.DoesNotContain("Name", output);
    }

    [Fact]
    public void IncludeFields_CaseInsensitiveByDefault()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["name"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"));
        var output = orch.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Field projection: ExcludeFields ---

    [Fact]
    public void ExcludeFields_RemovesSpecifiedFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeFields = ["Version"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("License", "MIT"));
        var output = orch.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("License: MIT", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Field projection: FieldList ---

    [Fact]
    public void IncludeFields_FiltersFieldList()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Name", "TFM"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFieldsInline(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("TFM", "net8.0"));
        var output = orch.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("TFM: net8.0", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeFields_ReordersFieldList()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["TFM", "Name"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFieldsInline(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("TFM", "net8.0"));
        var output = orch.ToString();
        int tfmIndex = output.IndexOf("TFM");
        int nameIndex = output.IndexOf("Name");
        Assert.True(tfmIndex < nameIndex, "TFM should appear before Name in field list");
    }

    [Fact]
    public void ExcludeFields_FiltersFieldList()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeFields = ["Version"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFieldsInline(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("TFM", "net8.0"));
        var output = orch.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("TFM: net8.0", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeFields_AllFilteredProducesNoOutput()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["NonExistent"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields([new("Name", "Foo")]);
        orch.WriteFieldsInline(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"));
        var output = orch.ToString();
        Assert.Equal("", output);
    }

    // --- Projection composes with section filtering ---

    [Fact]
    public void Projection_ComposesWithSectionFiltering()
    {
        var options = new MarkoutWriterOptions
        {
            IncludeSections = ["Details"],
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteHeading(1, "Report");
        orch.WriteFields([new("TopLevel", "value")]);
        orch.WriteHeading(2, "Details");
        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo", "1.0.0");
        orch.WriteTableEnd();
        orch.WriteHeading(2, "Other");
        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Bar", "2.0.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        // Details section included, Other excluded
        Assert.Contains("Foo", output);
        Assert.DoesNotContain("Bar", output);
        // Column projection applied within Details
        Assert.DoesNotContain("1.0.0", output);
    }

    // --- Mutual exclusion validation ---

    [Fact]
    public void IncludeAndExcludeColumns_ThrowsWhenBothSet()
    {
        var projection = new MarkoutProjection
        {
            IncludeColumns = ["Name"]
        };
        Assert.Throws<InvalidOperationException>(() =>
            projection.ExcludeColumns = ["Version"]);
    }

    [Fact]
    public void IncludeAndExcludeFields_ThrowsWhenBothSet()
    {
        var projection = new MarkoutProjection
        {
            IncludeFields = ["Name"]
        };
        Assert.Throws<InvalidOperationException>(() =>
            projection.ExcludeFields = ["Version"]);
    }

    // --- No projection = passthrough ---

    [Fact]
    public void NoProjection_TablePassesThrough()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo", "1.0.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Version", output);
        Assert.Contains("Foo", output);
        Assert.Contains("1.0.0", output);
    }

    [Fact]
    public void NoProjection_FieldsPassThrough()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteFields(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"));
        var output = orch.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("Version: 1.0.0", output);
    }

    // --- Column projection with MaxItems ---

    [Fact]
    public void IncludeColumns_ComposesWithMaxItems()
    {
        var options = new MarkoutWriterOptions
        {
            MaxItems = 1,
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo", "1.0.0");
        orch.WriteTableRow("Bar", "2.0.0");
        orch.WriteTableRow("Baz", "3.0.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Foo", output);
        Assert.DoesNotContain("Bar", output);
        Assert.Contains("... and 2 more", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Schema discovery ---

    [Fact]
    public void GetFieldNames_ReturnsDocumentLevelFields()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var fields = schema.GetFieldNames();
        Assert.Equal(["Name", "Version"], fields);
    }

    [Fact]
    public void GetColumnNames_ReturnsUniqueColumnsAcrossTables()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var columns = schema.GetColumnNames();
        // Dependencies has Id, Version; Assemblies has Name, Arch
        Assert.Equal(["Id", "Version", "Name", "Arch"], columns);
    }

    [Fact]
    public void GetSectionNames_ReturnsSectionHeadingNames()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var sections = schema.GetSectionNames();
        Assert.Equal(["Dependencies", "Assemblies"], sections);
    }

    [Fact]
    public void ExtractSectionName_ParsesSectionRendering()
    {
        Assert.Equal("Dependencies", MarkoutSchemaInfo.ExtractSectionName("H2 Section \"Dependencies\" (table)"));
        Assert.Equal("API Surface", MarkoutSchemaInfo.ExtractSectionName("H2 Section \"API Surface\" (subsections)"));
        Assert.Null(MarkoutSchemaInfo.ExtractSectionName("Field"));
        Assert.Null(MarkoutSchemaInfo.ExtractSectionName("Table"));
    }

    [Fact]
    public void GetFieldNames_EmptyForTableOnlySchema()
    {
        // A type with no scalar fields should return empty
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "NoFields",
            AsDocument = [
                new() { Name = "Items", DisplayName = "Items", Rendering = "H2 Section \"Items\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                ] },
            ],
        };
        Assert.Empty(schema.GetFieldNames());
    }

    [Fact]
    public void GetColumnNames_EmptyForFieldOnlySchema()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "FieldsOnly",
            AsDocument = [
                new() { Name = "Name", DisplayName = "Name", Rendering = "Field" },
                new() { Name = "Version", DisplayName = "Version", Rendering = "Field" },
            ],
        };
        Assert.Empty(schema.GetColumnNames());
    }

    [Fact]
    public void GetColumnNames_DeduplicatesAcrossTables()
    {
        // Two tables both have a "Name" column — should appear once
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "DupCols",
            AsDocument = [
                new() { Name = "Deps", DisplayName = "Deps", Rendering = "H2 Section \"Deps\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                    new() { Name = "Version", DisplayName = "Version", Rendering = "Column" },
                ] },
                new() { Name = "Refs", DisplayName = "Refs", Rendering = "H2 Section \"Refs\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                    new() { Name = "Target", DisplayName = "Target", Rendering = "Column" },
                ] },
            ],
        };
        var columns = schema.GetColumnNames();
        Assert.Equal(["Name", "Version", "Target"], columns);
    }

    // --- ToDocumentSchema tests ---

    [Fact]
    public void ToDocumentSchema_TableSections_ProducesColumns()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var doc = schema.ToDocumentSchema();

        Assert.Equal(["Dependencies", "Assemblies"], doc.SectionNames);

        var deps = doc.GetSection("Dependencies");
        Assert.NotNull(deps);
        Assert.Equal("column", deps.ItemKind);
        Assert.Equal(["Id", "Version"], deps.Items.Select(i => i.Name).ToArray());

        var asms = doc.GetSection("Assemblies");
        Assert.NotNull(asms);
        Assert.Equal("column", asms.ItemKind);
        Assert.Equal(["Name", "Arch"], asms.Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public void ToDocumentSchema_ScalarFieldSections_ProducesFields()
    {
        var schema = ScalarSectionTestContext.Default.GetSchemaInfo<MixedSection>()!;
        var doc = schema.ToDocumentSchema();

        Assert.Equal(["Stats", "Dependencies"], doc.SectionNames);

        var stats = doc.GetSection("Stats");
        Assert.NotNull(stats);
        Assert.Equal("field", stats.ItemKind);
        Assert.Contains("Downloads", stats.Items.Select(i => i.Name));
        Assert.Contains("Stars", stats.Items.Select(i => i.Name));

        var deps = doc.GetSection("Dependencies");
        Assert.NotNull(deps);
        Assert.Equal("column", deps.ItemKind);
    }

    [Fact]
    public void ToDocumentSchema_MergesDuplicateSections()
    {
        // Two properties sharing "Items" section with different columns
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "MergeTest",
            AsDocument = [
                new() { Name = "ItemsA", DisplayName = "Items", Rendering = "H2 Section \"Items\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                    new() { Name = "Version", DisplayName = "Version", Rendering = "Column" },
                ] },
                new() { Name = "ItemsB", DisplayName = "Items", Rendering = "H2 Section \"Items\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                    new() { Name = "Arch", DisplayName = "Arch", Rendering = "Column" },
                ] },
            ],
        };
        var doc = schema.ToDocumentSchema();

        Assert.Single(doc.SectionNames);
        var items = doc.GetSection("Items")!;
        Assert.Equal("column", items.ItemKind);
        Assert.Equal(["Name", "Version", "Arch"], items.Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public void ToDocumentSchema_FieldSection_SingleScalar()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "SingleField",
            AsDocument = [
                new() { Name = "Url", DisplayName = "URL", Rendering = "H2 Section \"Remote Source\" (field)" },
            ],
        };
        var doc = schema.ToDocumentSchema();

        var section = doc.GetSection("Remote Source")!;
        Assert.Equal("field", section.ItemKind);
        Assert.Equal(["URL"], section.Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public void ToDocumentSchema_NestedFields_CollectsChildFields()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "Nested",
            AsDocument = [
                new() { Name = "Info", DisplayName = "Info", Rendering = "H2 Section \"Library Info\" (fields)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Field" },
                    new() { Name = "Version", DisplayName = "Version", Rendering = "Field" },
                    new() { Name = "IsSigned", DisplayName = "Signed", Rendering = "Field (yes/no)" },
                ] },
            ],
        };
        var doc = schema.ToDocumentSchema();

        var section = doc.GetSection("Library Info")!;
        Assert.Equal("field", section.ItemKind);
        Assert.Equal(["Name", "Version", "Signed"], section.Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public void ToDocumentSchema_TreeSection_CollectsColumns()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "TreeTest",
            AsDocument = [
                new() { Name = "Deps", DisplayName = "Deps", Rendering = "H2 Section \"Dependencies\" (tree)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                ] },
            ],
        };
        var doc = schema.ToDocumentSchema();

        var section = doc.GetSection("Dependencies")!;
        Assert.Equal("tree", section.ItemKind);
        Assert.Equal(["Name"], section.Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public void ToDocumentSchema_CodeBlockSection_SectionOnly()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "CodeTest",
            AsDocument = [
                new() { Name = "IL", DisplayName = "IL", Rendering = "H2 Section \"IL\" (code block)" },
            ],
        };
        var doc = schema.ToDocumentSchema();

        Assert.Equal(["IL"], doc.SectionNames);
        var section = doc.GetSection("IL")!;
        Assert.Equal("section", section.ItemKind);
        Assert.Empty(section.Items);
    }

    [Fact]
    public void ToDocumentSchema_FieldTableSection_SectionOnly()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "FieldTableTest",
            AsDocument = [
                new() { Name = "Summary", DisplayName = "Summary", Rendering = "H2 Section \"Summary\" (field table)" },
            ],
        };
        var doc = schema.ToDocumentSchema();

        Assert.Equal(["Summary"], doc.SectionNames);
        var section = doc.GetSection("Summary")!;
        Assert.Equal("section", section.ItemKind);
        Assert.Empty(section.Items);
    }

    [Fact]
    public void ToDocumentSchema_SkipsNonSectionProperties()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "MixedProps",
            AsDocument = [
                new() { Name = "Name", DisplayName = "Name", Rendering = "Field" },
                new() { Name = "Version", DisplayName = "Version", Rendering = "Field" },
                new() { Name = "Deps", DisplayName = "Deps", Rendering = "H2 Section \"Dependencies\" (table)", Children = [
                    new() { Name = "Id", DisplayName = "Id", Rendering = "Column" },
                ] },
            ],
        };
        var doc = schema.ToDocumentSchema();

        // Only the section should appear, not the scalar fields
        Assert.Equal(["Dependencies"], doc.SectionNames);
    }

    [Fact]
    public void ToDocumentSchema_MergesFieldAndCodeBlock()
    {
        // A section with both scalar fields and a code block property
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "MixedKinds",
            AsDocument = [
                new() { Name = "Sig", DisplayName = "Signature", Rendering = "H2 Section \"Constructors\" (code block)" },
                new() { Name = "Table", DisplayName = "Table", Rendering = "H2 Section \"Constructors\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                ] },
            ],
        };
        var doc = schema.ToDocumentSchema();

        Assert.Single(doc.SectionNames);
        var section = doc.GetSection("Constructors")!;
        // Code block came first (no items), table merged in with items → upgraded
        Assert.Equal("column", section.ItemKind);
        Assert.Equal(["Name"], section.Items.Select(i => i.Name).ToArray());
    }

    // --- Factory method tests ---

    [Fact]
    public void WithColumns_CreatesProjectionWithIncludeColumns()
    {
        var projection = MarkoutProjection.WithColumns("Name", "TFM");
        Assert.Equal(["Name", "TFM"], projection.IncludeColumns);
        Assert.Null(projection.ExcludeColumns);
    }

    [Fact]
    public void WithoutColumns_CreatesProjectionWithExcludeColumns()
    {
        var projection = MarkoutProjection.WithoutColumns("Version", "Signed");
        Assert.Null(projection.IncludeColumns);
        Assert.Contains("Version", projection.ExcludeColumns!);
        Assert.Contains("Signed", projection.ExcludeColumns!);
    }

    [Fact]
    public void WithFields_CreatesProjectionWithIncludeFields()
    {
        var projection = MarkoutProjection.WithFields("Name", "License");
        Assert.Equal(["Name", "License"], projection.IncludeFields);
        Assert.Null(projection.ExcludeFields);
    }

    [Fact]
    public void WithoutFields_CreatesProjectionWithExcludeFields()
    {
        var projection = MarkoutProjection.WithoutFields("InternalId");
        Assert.Null(projection.IncludeFields);
        Assert.Contains("InternalId", projection.ExcludeFields!);
    }

    [Fact]
    public void FactoryMethod_WorksWithWriterOptions()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithColumns("Name")
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name", "Version");
        orch.WriteTableRow("Foo", "1.0.0");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo", output);
        Assert.DoesNotContain("Version", output);
    }
}
