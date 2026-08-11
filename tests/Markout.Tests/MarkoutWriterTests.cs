using Markout;
using Markout.Formatting;
using MarkdownTable.Formatting;

namespace Markout.Tests;

public class MarkoutWriterTests
{
    // ── MarkdownFormatter formatter — all shapes render ──

    [Fact]
    public void MarkdownFormatter_WriteHeading_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteHeading(1, "Title");
        Assert.True(result);
        Assert.Equal("# Title", orch.ToString());
    }

    [Fact]
    public void MarkdownFormatter_WriteHeading_WithContext()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteHeading(2, "Section", "v1.0");
        Assert.True(result);
        Assert.Equal("## Section (v1.0)", orch.ToString());
    }

    [Fact]
    public void WriteHeading_InvalidLevel_Throws()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        Assert.Throws<ArgumentOutOfRangeException>(() => orch.WriteHeading(0, "Bad"));
        Assert.Throws<ArgumentOutOfRangeException>(() => orch.WriteHeading(7, "Bad"));
    }

    [Fact]
    public void HeadingLevelOffset_ShiftsHeadingDown()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { HeadingLevelOffset = 1 });
        orch.WriteHeading(1, "Title");
        Assert.Equal("## Title", orch.ToString());
    }

    [Fact]
    public void HeadingLevelOffset_ShiftsSectionHeadingDown()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { HeadingLevelOffset = 1 });
        orch.WriteSectionStart(2, "Section");
        Assert.Equal("### Section", orch.ToString());
    }

    [Fact]
    public void HeadingLevelOffset_ClampsToSix()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { HeadingLevelOffset = 3 });
        orch.WriteHeading(5, "Deep");
        Assert.Equal("###### Deep", orch.ToString());
    }

    [Fact]
    public void HeadingLevelOffset_NegativeClampsToOne()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { HeadingLevelOffset = -5 });
        orch.WriteHeading(2, "Up");
        Assert.Equal("# Up", orch.ToString());
    }

    [Fact]
    public void HeadingLevelOffset_DefaultZero_LeavesLevelUnchanged()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteHeading(1, "Title");
        Assert.Equal("# Title", orch.ToString());
    }

    [Fact]
    public void HeadingLevelOffset_DoesNotChangeLogicalSectionFiltering()
    {
        // IncludeSections keys off the logical level-2 section name, which must
        // still match even when the rendered heading is shifted by the offset.
        var options = new MarkoutWriterOptions
        {
            HeadingLevelOffset = 1,
            IncludeSections = ["Keep"],
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteSectionStart(2, "Keep");
        orch.WriteParagraph("kept");
        orch.WriteSectionStart(2, "Drop");
        orch.WriteParagraph("dropped");

        var output = orch.ToString();
        Assert.Contains("### Keep", output);
        Assert.Contains("kept", output);
        Assert.DoesNotContain("Drop", output);
        Assert.DoesNotContain("dropped", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteFields_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteFields(new MarkoutField("Name", "Alice"), new MarkoutField("Age", "30"));
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Name: Alice", output);
        Assert.Contains("Age: 30", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteFields_Bold()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions { BoldFieldNames = true });
        orch.WriteFields(new MarkoutField("Name", "Alice"));
        var output = orch.ToString();
        Assert.Contains("**Name:** Alice", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteTable_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteTable(["Name", "Age"], [["Alice", "30"], ["Bob", "25"]]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("| Name |", output);
        Assert.Contains("| Alice |", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteTable_AppliesHeaderFormatter()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            FormatTableHeader = header => $"{header.Name}:{header.DisplayName}:{header.Index}"
        });

        var result = orch.WriteTable(
            ["Display Name", "Age"],
            ["Name", "Age"],
            [["Alice", "30"]]);

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("| Name:Display Name:0 | Age:Age:1 |", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteTableStart_AppliesHeaderFormatterAfterProjection()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["Age"] },
            FormatTableHeader = header => $"{header.Name}:{header.DisplayName}:{header.Index}"
        });

        Assert.True(orch.WriteTableStart(["Display Name", "Age"], ["Name", "Age"]));
        orch.WriteTableRow("Alice", "30");
        orch.WriteTableEnd();

        var output = orch.ToString();
        Assert.DoesNotContain("Display Name", output);
        Assert.Contains("| Age:Age:0 |", output);
        Assert.Contains("| 30 |", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteCodeBlock_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        Assert.True(orch.WriteCodeStart("csharp"));
        orch.WriteBlankLine(); // just to have some content
        Assert.True(orch.WriteCodeEnd());
        var output = orch.ToString();
        Assert.Contains("```csharp", output);
        Assert.Contains("```", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteCallout_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteCallout(CalloutSeverity.Warning, "Watch out!");
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("[!WARNING]", output);
        Assert.Contains("Watch out!", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteQuotation_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteQuotation("To be or not to be");
        Assert.True(result);
        Assert.Contains("> To be or not to be", orch.ToString());
    }

    [Fact]
    public void MarkdownFormatter_WriteRule_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteParagraph("Before");
        var result = orch.WriteRule();
        Assert.True(result);
        Assert.Contains("---", orch.ToString());
    }

    [Fact]
    public void MarkdownFormatter_WriteDescriptions_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteDescriptions([new Description("Term", "Definition")]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("**Term:**", output);
        Assert.Contains("Definition", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteBreakdown_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var breakdown = new Breakdown("Test", [new Slice("Pass", 8), new Slice("Fail", 2)]);
        var result = orch.WriteBreakdown([breakdown]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Category", output);
        Assert.Contains("Pass", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteMetrics_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteMetrics([new Metric("CPU", 75), new Metric("Mem", 50)]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Label", output);
        Assert.Contains("CPU", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteListItem_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteListItem("Item one");
        Assert.True(result);
        Assert.Contains("- Item one", orch.ToString());
    }

    [Fact]
    public void MarkdownFormatter_WriteList_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteList("A", "B", "C");
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("- A", output);
        Assert.Contains("- B", output);
        Assert.Contains("- C", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteArray_WithKey_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteArray("Items", "X", "Y");
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Items:", output);
        Assert.Contains("- X", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteParagraph_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteParagraph("Hello world");
        Assert.True(result);
        Assert.Equal("Hello world", orch.ToString());
    }

    [Fact]
    public void MarkdownFormatter_WriteTree_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteTree(new TreeNode("Root", [new TreeNode("Child")]));
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Child", output);
    }

    [Fact]
    public void MarkdownFormatter_WriteTreeNode_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteTreeNode("node text", ">> ");
        Assert.True(result);
        Assert.Contains(">> node text", orch.ToString());
    }

    // ── TableFormatter formatter — subset renders ──

    [Fact]
    public void TableFormatter_WriteHeading_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteHeading(1, "Title");
        Assert.False(result);
    }

    [Fact]
    public void TableFormatter_WriteTable_ReturnsTrue()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteTable(["Col"], [["Val"]]);
        Assert.True(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TableFormatter_Jsonl_SectionsFormOneRowStream(bool streaming)
    {
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });

        writer.WriteSectionStart(2, "Alpha", headless: true);
        if (streaming)
        {
            writer.WriteTableStart("name");
            writer.WriteTableRow("a1");
            writer.WriteTableRow("a2");
        }
        else
        {
            writer.WriteTable(["name"], [["a1"], ["a2"]]);
        }

        writer.WriteSectionStart(2, "Beta", headless: true);
        if (streaming)
        {
            writer.WriteTableStart("name");
            writer.WriteTableRow("b1");
        }
        else
        {
            writer.WriteTable(["name"], [["b1"]]);
        }

        Assert.Equal(
            "{\"name\":\"a1\"}\n{\"name\":\"a2\"}\n{\"name\":\"b1\"}",
            writer.Complete().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TableFormatter_WriteFields_ReturnsTrue()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteFields(new MarkoutField("K", "V"));
        Assert.True(result);
    }

    [Fact]
    public void TableFormatter_WriteCallout_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteCallout(CalloutSeverity.Note, "msg");
        Assert.False(result);
    }

    [Fact]
    public void TableFormatter_WriteCodeStart_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteCodeStart("csharp");
        Assert.False(result);
    }

    [Fact]
    public void TableFormatter_WriteBreakdown_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteBreakdown([new Breakdown("X", [new Slice("A", 1)])]);
        Assert.False(result);
    }

    [Fact]
    public void TableFormatter_WriteRule_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new TableFormatter());
        var result = orch.WriteRule();
        Assert.False(result);
    }

    // ── Section filtering ──

    [Fact]
    public void IncludeSections_FiltersContent()
    {
        var options = new MarkoutWriterOptions { IncludeSections = ["Visible"] };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteHeading(1, "Title");
        orch.WriteHeading(2, "Visible");
        orch.WriteParagraph("Included");
        orch.WriteHeading(2, "Hidden");
        orch.WriteParagraph("Excluded");

        var output = orch.ToString();
        Assert.Contains("Included", output);
        Assert.DoesNotContain("Excluded", output);
        Assert.Contains("## Visible", output);
        Assert.DoesNotContain("## Hidden", output);
    }

    [Fact]
    public void EmptyIncludeSections_PreambleOnly()
    {
        var options = new MarkoutWriterOptions { IncludeSections = [] };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteHeading(1, "Title");
        orch.WriteParagraph("Preamble");
        orch.WriteHeading(2, "Section");
        orch.WriteParagraph("SectionContent");

        var output = orch.ToString();
        Assert.Contains("Preamble", output);
        Assert.DoesNotContain("SectionContent", output);
    }

    // ── Field projection ──

    [Fact]
    public void Projection_IncludeFields_FiltersFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithFields("Name")
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(new MarkoutField("Name", "Alice"), new MarkoutField("Age", "30"));

        var output = orch.ToString();
        Assert.Contains("Name: Alice", output);
        Assert.DoesNotContain("Age", output);
    }

    [Fact]
    public void Projection_ExcludeFields_FiltersFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithoutFields("Age")
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteFields(new MarkoutField("Name", "Alice"), new MarkoutField("Age", "30"));

        var output = orch.ToString();
        Assert.Contains("Name: Alice", output);
        Assert.DoesNotContain("Age", output);
    }

    [Fact]
    public void Projection_IncludeColumns_FiltersTable()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithColumns("Name")
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTable(["Name", "Age"], [["Alice", "30"]]);

        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.DoesNotContain("Age", output);
    }

    // ── Field-to-table cascade ──

    [Fact]
    public void FieldCascade_TableOnlyFormatter_RendersFieldsAsTable()
    {
        var orch = MarkoutWriter.Create(new TableOnlyFormatter());

        orch.WriteFields(new MarkoutField("A", "1"), new MarkoutField("B", "2"));

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("Value", output);
        Assert.Contains("A", output);
        Assert.Contains("B", output);
    }

    [Fact]
    public void FieldCascade_FullFormatter_RendersFieldsNatively()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());

        orch.WriteFields(new MarkoutField("A", "1"));

        var output = orch.ToString();
        // MarkdownFormatter implements IFieldFormatter, so fields render as key: value, not as a table
        Assert.Contains("A:", output);
        Assert.DoesNotContain("| Field |", output);
    }

    [Fact]
    public void FieldCascade_WriteFieldsInline_FallsBackToTable()
    {
        var orch = MarkoutWriter.Create(new TableOnlyFormatter());

        orch.WriteFieldsInline(new MarkoutField("A", "1"), new MarkoutField("B", "2"));

        var output = orch.ToString();
        // No IFieldFormatter, so inline falls back to table
        Assert.Contains("Field", output);
        Assert.Contains("A", output);
    }

    [Fact]
    public void FieldCascade_WriteFieldsBulleted_FallsBackToTable()
    {
        var orch = MarkoutWriter.Create(new TableOnlyFormatter());

        orch.WriteFieldsBulleted(new MarkoutField("X", "Y"));

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("X", output);
    }

    [Fact]
    public void FieldCascade_WriteFieldsNumbered_FallsBackToTable()
    {
        var orch = MarkoutWriter.Create(new TableOnlyFormatter());

        orch.WriteFieldsNumbered(new MarkoutField("X", "Y"));

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("X", output);
    }

    [Fact]
    public void FieldCascade_WriteField_FallsBackToTable()
    {
        var orch = MarkoutWriter.Create(new TableOnlyFormatter());

        orch.WriteField("Key", "Val");

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("Key", output);
    }

    [Fact]
    public void FieldCascade_MinimalFormatter_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MinimalFormatter());

        var result = orch.WriteFields(new MarkoutField("A", "1"));

        Assert.False(result);
    }

    // ── MaxItems ──

    [Fact]
    public void MaxItems_LimitsTableRows()
    {
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteTable(["Name"], [["Alice"], ["Bob"], ["Carol"]]);

        var output = orch.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void MaxItems_StreamingTable()
    {
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteTableStart("Name");
        orch.WriteTableRow("Alice");
        orch.WriteTableRow("Bob");
        orch.WriteTableRow("Carol");
        orch.WriteTableEnd();

        var output = orch.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    // ── Pending section with projection ──

    [Fact]
    public void PendingSection_DefersHeadingUntilContent()
    {
        var options = new MarkoutWriterOptions
        {
            IncludeSections = ["Data"],
            Projection = new MarkoutProjection()
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        // Only "Data" section should appear in output
        orch.WriteSectionStart(2, "Data");
        // No content yet — heading should be deferred
        orch.WriteFields(new MarkoutField("Key", "Value"));
        orch.WriteSectionEnd();

        var output = orch.ToString();
        Assert.Contains("## Data", output);
        Assert.Contains("Key: Value", output);
    }

    [Fact]
    public void PendingSection_SuppressedWhenNoContent()
    {
        var options = new MarkoutWriterOptions
        {
            IncludeSections = ["Data"],
            Projection = new MarkoutProjection()
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteSectionStart(2, "Data");
        // No content written — heading should not appear
        orch.WriteSectionEnd();

        var output = orch.ToString();
        Assert.Equal("", output);
    }

    // ── Output parity with MarkdownFormatter ──

    [Fact]
    public void OutputParity_Heading()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteHeading(1, "Title");
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteHeading(1, "Title");
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_Fields()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteFields(new MarkoutField("A", "1"), new MarkoutField("B", "2"));
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteFields(new MarkoutField("A", "1"), new MarkoutField("B", "2"));
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_Table()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteTable(["Name", "Age"], [["Alice", "30"]]);
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTable(["Name", "Age"], [["Alice", "30"]]);
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_HeadingThenFields()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteHeading(1, "Title");
        writer.WriteFields(new MarkoutField("K", "V"));
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteHeading(1, "Title");
        orch.WriteFields(new MarkoutField("K", "V"));
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_HeadingThenTable()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteHeading(2, "Data");
        writer.WriteTable(["X"], [["1"]]);
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteHeading(2, "Data");
        orch.WriteTable(["X"], [["1"]]);
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_ListItems()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteList("A", "B");
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteList("A", "B");
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_CodeBlock()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteCodeStart("json");
        writer.WriteCodeEnd();
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteCodeStart("json");
        orch.WriteCodeEnd();
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_Callout()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteCallout(CalloutSeverity.Note, "Info");
        var expected = writer.ToString();

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteCallout(CalloutSeverity.Note, "Info");
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    // ── ToString ──

    [Fact]
    public void ToString_TrimsTrailingWhitespace()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteParagraph("Text");
        orch.WriteBlankLine();

        var output = orch.ToString();
        Assert.Equal("Text", output);
    }

    [Fact]
    public void ToString_RendersFieldOutput()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());

        orch.WriteFields(new MarkoutField("Key", "Val"));

        var output = orch.ToString();
        Assert.Contains("Key", output);
        Assert.Contains("Val", output);
    }

    // ── WriteLinkDefinitions ──

    [Fact]
    public void WriteLinkDefinitions_RendersDefinitions()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteLinkDefinitions("[0]: https://example.com", "[1]: https://other.com");
        Assert.Equal("[0]: https://example.com\n[1]: https://other.com", orch.ToString());
    }

    [Fact]
    public void WriteLinkDefinitions_BlankLineAfterTable()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTable(["Name"], [["Alice"]]);
        orch.WriteLinkDefinitions("[0]: https://example.com");

        var output = orch.ToString();
        Assert.Contains("| Alice |\n\n[0]:", output);
    }

    [Fact]
    public void WriteLinkDefinitions_BlankLineBeforeHeading()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteLinkDefinitions("[0]: https://example.com");
        orch.WriteHeading(2, "Next");

        var output = orch.ToString();
        Assert.Contains("[0]: https://example.com\n\n## Next", output);
    }

    [Fact]
    public void WriteLinkDefinitions_EmptySpan_NoOutput()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteParagraph("Text");
        orch.WriteLinkDefinitions();
        Assert.Equal("Text", orch.ToString());
    }

    [Fact]
    public void WriteLinkDefinitions_BetweenTableAndHeading()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTable(["Col"], [["Val"]]);
        orch.WriteLinkDefinitions("[0]: https://example.com", "[1]: https://other.com");
        orch.WriteHeading(2, "Section");

        var output = orch.ToString();
        // Blank line after table, contiguous definitions, blank line before heading
        Assert.Contains("| Val |\n\n[0]: https://example.com\n[1]: https://other.com\n\n## Section", output);
    }

    // ── TableOptions (statistical width calculation) ──

    [Fact]
    public void PrettyTable_WithTableOptions_CapsOutlierColumnWidth()
    {
        var options = new MarkoutWriterOptions
        {
            PrettyTables = true,
            TableOptions = new TableFormatterOptions()
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTableStart("OS", "Versions", "Arch");
        writer.WriteTableRow("Alpine", "3.21, 3.20", "x64, Arm64");
        writer.WriteTableRow("Ubuntu", "24.04", "x64, Arm64");
        writer.WriteTableRow("Windows", "11 26H1, 11 25H2, 11 24H2 (IoT), 11 24H2 (E), 11 24H2, 11 23H2 (E), 10 21H2 (E), 10 21H2 (IoT), 10 1809 (E), 10 1607 (E)", "x64, Arm64");
        writer.WriteTableRow("Debian", "12", "x64, Arm64");
        writer.WriteTableEnd();

        var output = writer.ToString();
        var lines = output.Split('\n');

        // Header separator should NOT be as wide as the Windows outlier row
        var separatorLine = lines[1];
        // Without statistical widths, the separator would be ~140+ chars (matching Windows row)
        // With statistical widths, it should be much shorter
        Assert.True(separatorLine.Length < 80, $"Separator line too wide ({separatorLine.Length} chars): {separatorLine}");

        // The Windows row should overflow (wider than separator)
        var windowsRow = lines.First(l => l.Contains("Windows"));
        Assert.True(windowsRow.Length > separatorLine.Length, "Windows row should overflow the calculated width");
    }

    [Fact]
    public void PrettyTable_WithoutTableOptions_UsesMaxWidth()
    {
        var options = new MarkoutWriterOptions { PrettyTables = true };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTable(
            ["OS", "Versions", "Arch"],
            [["Alpine", "3.21, 3.20", "x64"],
             ["Windows", "11 26H1, 11 25H2, 11 24H2 (IoT), 11 24H2 (E)", "x64"]]);

        var output = writer.ToString();
        var lines = output.Split('\n');

        // All rows should be the same width (max-width padding)
        var pipeLines = lines.Where(l => l.StartsWith('|')).ToList();
        Assert.True(pipeLines.All(l => l.Length == pipeLines[0].Length),
            "All rows should have equal width with simple max-width");
    }

    [Fact]
    public void PrettyTable_Batch_WithTableOptions_CapsOutlierWidth()
    {
        var options = new MarkoutWriterOptions
        {
            PrettyTables = true,
            TableOptions = new TableFormatterOptions()
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        var headers = new[] { "Name", "Description" };
        var rows = new List<string[]>
        {
            new[] { "Short", "Brief" },
            new[] { "Also short", "Brief" },
            new[] { "Outlier", "This is a very long description that should be treated as an outlier and not stretch the entire column width for all other rows in the table" }
        };

        writer.WriteTable(headers, rows);

        var output = writer.ToString();
        var lines = output.Split('\n');

        var separatorLine = lines[1];
        var outlierRow = lines.First(l => l.Contains("Outlier"));
        Assert.True(outlierRow.Length > separatorLine.Length,
            "Outlier row should overflow the statistical column width");
    }

    [Fact]
    public void StreamingTable_WithTableOptions_BuffersForBatchRender()
    {
        // When TableOptions is set, streaming tables should buffer and render
        // through the batch path (same result as WriteTable)
        var options = new MarkoutWriterOptions
        {
            PrettyTables = true,
            TableOptions = new TableFormatterOptions()
        };

        // Streaming path
        var streamWriter = MarkoutWriter.Create(new MarkdownFormatter(), options);
        streamWriter.WriteTableStart("A", "B");
        streamWriter.WriteTableRow("short", "x");
        streamWriter.WriteTableRow("also short", "y");
        streamWriter.WriteTableRow("outlier value that is much longer than the others", "z");
        streamWriter.WriteTableEnd();

        // Batch path
        var batchWriter = MarkoutWriter.Create(new MarkdownFormatter(), options);
        batchWriter.WriteTable(
            ["A", "B"],
            [["short", "x"], ["also short", "y"], ["outlier value that is much longer than the others", "z"]]);

        Assert.Equal(batchWriter.ToString(), streamWriter.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StreamingTable_NestedStartCompletesTheOpenTable(bool buffered)
    {
        var options = new MarkoutWriterOptions();
        if (buffered)
            options.TableOptions = new TableFormatterOptions();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTableStart("A");
        writer.WriteTableRow("first");
        writer.WriteTableStart("B");
        writer.WriteTableRow("second");
        writer.WriteTableEnd();

        Assert.Equal(
            "| A |\n| - |\n| first |\n\n| B |\n| - |\n| second |",
            writer.ToString());
    }

    [Fact]
    public void BufferedTable_ProjectionMissStillCompletesAnOpenStreamingTable()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithColumns("A"),
            TableOptions = new TableFormatterOptions()
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTableStart("A");
        writer.WriteTableRow("first");
        writer.WriteTable(["B"], [["dropped"]]);

        Assert.Equal("| A |\n| - |\n| first |", writer.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StreamingTable_LaterBlockCompletesTheOpenTableInCallOrder(bool buffered)
    {
        var options = new MarkoutWriterOptions();
        if (buffered)
            options.TableOptions = new TableFormatterOptions();
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteTableStart("A");
        writer.WriteTableRow("first");
        writer.WriteParagraph("after");

        Assert.Equal(
            "| A |\n| - |\n| first |\n\nafter",
            writer.ToString());
    }

    [Fact]
    public void StreamingTable_CompleteCompletesAnOpenBufferedTable()
    {
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TableOptions = new TableFormatterOptions() });

        writer.WriteTableStart("A");
        writer.WriteTableRow("first");
        Assert.Equal("", writer.ToString());
        writer.WriteTableRow("second");

        Assert.Equal("| A |\n| - |\n| first |\n| second |", writer.Complete());
    }

    [Fact]
    public void StreamingTable_FlushCompletesAnOpenBufferedTable()
    {
        var output = new StringWriter();
        var writer = new MarkoutWriter(
            output,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TableOptions = new TableFormatterOptions() });

        writer.WriteTableStart("A");
        writer.WriteTableRow("first");
        writer.Flush();

        Assert.Equal("| A |\n| - |\n| first |\n", output.ToString());
    }

    [Fact]
    public void StreamingTable_SectionBoundaryCompletesTheTableInItsStartingSection()
    {
        var options = new MarkoutWriterOptions
        {
            SectionOrder = ["Beta", "Alpha"],
            TableOptions = new TableFormatterOptions()
        };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteTableStart("A");
        writer.WriteTableRow("first");
        writer.WriteSectionStart(2, "Beta");
        writer.WriteParagraph("second");

        var output = writer.ToString();
        Assert.True(
            output.IndexOf("## Beta", StringComparison.Ordinal) <
            output.IndexOf("## Alpha", StringComparison.Ordinal));
        Assert.True(
            output.IndexOf("second", StringComparison.Ordinal) <
            output.IndexOf("| first |", StringComparison.Ordinal));
    }

    [Fact]
    public void PrettyTable_AutoTune_ExpandsAffordableBimodalColumns()
    {
        // Simulates the Out of Support table pattern:
        // OS column has a clear second mode (long names) that's affordable to expand
        // "Link" column has a second mode (long URLs) that's too expensive to expand
        var options = new MarkoutWriterOptions
        {
            PrettyTables = true,
            TableOptions = new TableFormatterOptions() { AutoTune = true }
        };

        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);
        writer.WriteTable(
            ["OS", "Ver", "Link"],
            [
                ["Alpine", "3.19", "2025-01-01"],
                ["Alpine", "3.18", "2024-05-01"],
                ["Fedora", "42", "2025-12-15"],
                ["Fedora", "41", "2025-05-13"],
                ["iOS", "18", "2025-09-15"],
                ["iOS", "17", "2024-11-19"],
                ["macOS", "15", "2025-09-15"],
                ["tvOS", "18", "2025-09-15"],
                ["tvOS", "17", "2024-09-16"],
                ["tvOS", "16", "2023-09-18"],
                ["Ubuntu", "24.04", "2029-05-31"],
                ["Ubuntu", "22.04", "2027-04-01"],
                // Second mode: long OS names (affordable expansion)
                ["openSUSE Leap", "15.5", "2024-12-31"],
                ["SUSE Linux Enterprise", "15.6", "2025-12-31"],
                // Second mode: long links (too expensive to expand)
                ["Windows", "10 22H2", "[2025-10-14](https://learn.microsoft.com/windows/release-health/release-information)"],
                ["Windows Server", "2012", "[2023-10-10](https://learn.microsoft.com/lifecycle/products/windows-server-2012)"],
            ]);

        var result = writer.ToString();
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Header should show expanded OS column (fits "SUSE Linux Enterprise" = 21 chars)
        Assert.StartsWith("| OS                    |", lines[0]);

        // Short OS names should be padded to the expanded width
        Assert.StartsWith("| Alpine                |", lines[2]);

        // Long OS names should fit without overflow
        Assert.StartsWith("| SUSE Linux Enterprise |", lines[15]);

        // Link column should NOT expand (too expensive) — long links overflow
        // The "Link" header column should stay narrow (statistical width)
        Assert.Contains("| Link          |", lines[0]);
    }

    [Fact]
    public void PrettyTable_AutoTune_ClustersTrailingPipes()
    {
        // Overflow rows should have trailing pipes clustered into tiers
        // rather than each row having a unique trailing position
        var options = new MarkoutWriterOptions
        {
            PrettyTables = true,
            TableOptions = new TableFormatterOptions() { AutoTune = true }
        };

        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);
        writer.WriteTable(
            ["OS", "Version", "End of Life"],
            [
                // Short rows — align with header
                ["tvOS", "18", "2025-09-15"],
                ["tvOS", "17", "2024-09-16"],
                ["tvOS", "16", "2023-09-18"],
                ["Fedora", "41", "2025-12-15"],
                ["Fedora", "40", "2025-05-13"],
                ["iOS", "17", "2024-11-19"],
                ["macOS", "13", "2025-09-15"],
                ["Ubuntu", "24.10", "2025-07-10"],
                ["openSUSE Leap", "15.5", "2024-12-31"],
                ["SUSE Linux Enterprise", "15.6", "2025-12-31"],
                // Overflow rows at various natural lengths — should cluster
                ["Android", "12.1", "[2025-03-03](https://developer.android.com/about/versions/12/12L)"],
                ["Windows Server", "2012", "[2023-10-10](https://learn.microsoft.com/lifecycle/products/windows-server-2012)"],
                ["Windows Server Core", "2012", "[2023-10-10](https://learn.microsoft.com/lifecycle/products/windows-server-2012)"],
                ["Windows", "11 23H2 (W)", "[2025-11-11](https://learn.microsoft.com/windows/release-health/windows11-release-information)"],
                ["Windows", "11 22H2 (E)", "[2025-10-14](https://learn.microsoft.com/windows/release-health/windows11-release-information)"],
                ["iOS", "16", "[2025-03-31](https://developer.apple.com/documentation/ios-ipados-release-notes/ios-16-release-notes)"],
                ["iPadOS", "16", "[2025-03-31](https://developer.apple.com/documentation/ios-ipados-release-notes/ipados-16-release-notes)"],
            ]);

        var result = writer.ToString();
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Count distinct line lengths — should be few tiers, not many unique values
        var lengths = lines.Select(l => l.Length).Distinct().OrderBy(x => x).ToList();

        // Without clustering we'd have 6+ unique lengths (55, ~88, ~108, ~122, ~136, ~146)
        // With clustering we expect ≤ 4 (header-aligned + 1-2 overflow tiers + separator)
        Assert.True(lengths.Count <= 4,
            $"Expected ≤ 4 distinct line lengths but got {lengths.Count}: [{string.Join(", ", lengths)}]");

        // All overflow rows should end with " |" (properly closed)
        foreach (var line in lines)
            Assert.EndsWith(" |", line);
    }

    // ── Static factory ──

    [Fact]
    public void Create_WithTextWriter_TypeInference()
    {
        var sw = new StringWriter();
        var orch = MarkoutWriter.Create(sw, new MarkdownFormatter());
        orch.WriteHeading(1, "Test");
        Assert.Contains("# Test", sw.ToString());
    }

    [Fact]
    public void Create_InMemory_TypeInference()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteParagraph("Hello");
        Assert.Equal("Hello", orch.ToString());
    }

    [Fact]
    public void ExplicitGeneric_Works()
    {
        var orch = new MarkoutWriter<MarkdownFormatter>(new MarkdownFormatter());
        orch.WriteHeading(1, "Explicit");
        Assert.Contains("# Explicit", orch.ToString());
    }

    // ── Streaming table ──

    [Fact]
    public void StreamingTable_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTableStart("Name", "Value");
        orch.WriteTableRow("A", "1");
        orch.WriteTableRow("B", "2");
        orch.WriteTableEnd();

        var output = orch.ToString();
        Assert.Contains("| Name |", output);
        Assert.Contains("| A |", output);
        Assert.Contains("| B |", output);
    }

    [Fact]
    public void StreamingTable_UnsupportedFormatter_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MinimalFormatter());
        var result = orch.WriteTableStart("Col");
        Assert.False(result);
    }

    [Fact]
    public void WriteTableRow_WithoutStart_Throws()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        Assert.Throws<InvalidOperationException>(() => orch.WriteTableRow("value"));
    }

    [Fact]
    public void WriteCodeEnd_WithoutStart_Throws()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        Assert.Throws<InvalidOperationException>(() => orch.WriteCodeEnd());
    }

    [Fact]
    public void WriteCodeStart_Nested_Throws()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteCodeStart();
        Assert.Throws<InvalidOperationException>(() => orch.WriteCodeStart());
    }

    // ── Minimal formatter (implements only IMarkoutFormatter) ──

    [Fact]
    public void MinimalFormatter_AllShapesReturnFalse()
    {
        var orch = MarkoutWriter.Create(new MinimalFormatter());

        Assert.False(orch.WriteHeading(1, "Title"));
        Assert.False(orch.WriteFields(new MarkoutField("K", "V")));
        Assert.False(orch.WriteTable(["H"], [["V"]]));
        Assert.False(orch.WriteListItem("item"));
        Assert.False(orch.WriteCallout(CalloutSeverity.Note, "msg"));
        Assert.False(orch.WriteRule());
        Assert.False(orch.WriteBreakdown([new Breakdown("X", [new Slice("A", 1)])]));
        Assert.False(orch.WriteMetrics([new Metric("M", 1)]));
        Assert.False(orch.WriteParagraph("text"));
        Assert.False(orch.WriteTreeNode("node"));
        Assert.False(orch.WriteTree(new TreeNode("root")));
    }

    // ── WriteFieldsTable ──

    [Fact]
    public void WriteFieldsTable_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteFieldsTable(new MarkoutField("Name", "Alice"));
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("| Field |", output);
        Assert.Contains("| Name |", output);
    }

    [Fact]
    public void WriteFieldsTable_UnsupportedFormatter_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MinimalFormatter());
        var result = orch.WriteFieldsTable(new MarkoutField("K", "V"));
        // WriteFieldsTable dispatches through WriteTable which checks ITableFormatter
        Assert.False(result);
    }

    // ── Column projection on streaming table ──

    [Fact]
    public void StreamingTable_WithColumnProjection()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithColumns("Name")
        };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);

        orch.WriteTableStart("Name", "Age");
        orch.WriteTableRow("Alice", "30");
        orch.WriteTableEnd();

        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.DoesNotContain("Age", output);
    }

    // ── WriteVerticalMetrics ──

    [Fact]
    public void WriteVerticalMetrics_Renders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        var result = orch.WriteVerticalMetrics([new Metric("A", 10), new Metric("B", 5)]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Label", output);
    }

    [Fact]
    public void WriteVerticalMetrics_UnsupportedFormatter_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MinimalFormatter());
        var result = orch.WriteVerticalMetrics([new Metric("A", 10)]);
        Assert.False(result);
    }

    // ── Flush ──

    [Fact]
    public void Flush_WritesOutputToStream()
    {
        var sw = new StringWriter();
        var orch = MarkoutWriter.Create(sw, new MarkdownFormatter());

        orch.WriteFields(new MarkoutField("K", "V"));
        orch.Flush();

        var output = sw.ToString();
        Assert.Contains("K", output);
    }

    // ── Streaming table via IStreamingTableFormatter ──

    [Fact]
    public void StreamingTable_DirectStreaming_WritesRowsImmediately()
    {
        var orch = MarkoutWriter.Create(new StreamingFormatter());

        orch.WriteTableStart("Name", "Age");
        orch.WriteTableRow("Alice", "30");
        orch.WriteTableRow("Bob", "25");
        orch.WriteTableEnd();

        var output = orch.ToString();
        Assert.Contains("[BEGIN]", output);
        Assert.Contains("Alice|30", output);
        Assert.Contains("Bob|25", output);
        Assert.Contains("[END:0]", output);
    }

    [Fact]
    public void StreamingTable_MaxItems_ReportsSkipped()
    {
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var orch = MarkoutWriter.Create(new StreamingFormatter(), options);

        orch.WriteTableStart("Name");
        orch.WriteTableRow("Alice");
        orch.WriteTableRow("Bob");
        orch.WriteTableRow("Carol");
        orch.WriteTableEnd();

        var output = orch.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("[END:2]", output);
    }

    [Fact]
    public void BatchTable_FallsBackToStreamingFormatter()
    {
        var orch = MarkoutWriter.Create(new StreamingFormatter());

        // WriteTable with IEnumerable (non-IList) should use streaming path
        IEnumerable<string[]> rows = GetRows();
        orch.WriteTable(["Name"], rows);

        var output = orch.ToString();
        Assert.Contains("[BEGIN]", output);
        Assert.Contains("Alice", output);
        Assert.Contains("[END:0]", output);

        static IEnumerable<string[]> GetRows()
        {
            yield return ["Alice"];
            yield return ["Bob"];
        }
    }

    [Fact]
    public void BatchTable_WithIList_UsesBatchFormatter()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());

        // IList<string[]> should take the batch path
        orch.WriteTable(["Name"], new List<string[]> { new[] { "Alice" } });

        var output = orch.ToString();
        Assert.Contains("| Name |", output);
        Assert.Contains("| Alice |", output);
    }

    // ── IDocumentFormatter aggregate ──

    [Fact]
    public void DocumentFormatter_MarkdownFormatter_ImplementsAggregate()
    {
        var writer = new MarkdownFormatter();
        Assert.IsAssignableFrom<IDocumentFormatter>(writer);
    }

    [Fact]
    public void DocumentFormatter_WorksAsOrchestratorConstraint()
    {
        // Verify MarkdownFormatter can be used via IDocumentFormatter
        var writer = new MarkdownFormatter();
        IDocumentFormatter df = writer;

        var sw = new StringWriter();
        df.FormatHeading(sw, 1, "Title", null);
        Assert.Contains("# Title", sw.ToString());
    }

    // ── UnicodeFormatter as orchestrator formatter ──

    [Fact]
    public void UnicodeFormatter_Orchestrator_WritesHeading()
    {
        var orch = MarkoutWriter.Create(new UnicodeFormatter());

        var result = orch.WriteHeading(1, "Test");

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Test", output);
    }

    [Fact]
    public void UnicodeFormatter_Orchestrator_WritesTable()
    {
        var orch = MarkoutWriter.Create(new UnicodeFormatter());

        var result = orch.WriteTable(["Name", "Age"], [["Alice", "30"]]);

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void UnicodeFormatter_Orchestrator_WritesFields()
    {
        var orch = MarkoutWriter.Create(new UnicodeFormatter());

        var result = orch.WriteFields(new MarkoutField("Status", "OK"));

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Status", output);
        Assert.Contains("OK", output);
    }

    [Fact]
    public void UnicodeFormatter_Orchestrator_WritesCallout()
    {
        var orch = MarkoutWriter.Create(new UnicodeFormatter());

        var result = orch.WriteCallout(CalloutSeverity.Warning, "Watch out!");

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Watch out!", output);
    }

    [Fact]
    public void UnicodeFormatter_Orchestrator_WritesMetrics()
    {
        var orch = MarkoutWriter.Create(new UnicodeFormatter());

        var result = orch.WriteMetrics([new Metric("CPU", 75)]);

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("CPU", output);
    }

    [Fact]
    public void UnicodeFormatter_Orchestrator_AllShapesReturn_True()
    {
        var orch = MarkoutWriter.Create(new UnicodeFormatter());

        Assert.True(orch.WriteHeading(1, "H1"));
        Assert.True(orch.WriteFields(new MarkoutField("K", "V")));
        Assert.True(orch.WriteTable(["H"], [["V"]]));
        Assert.True(orch.WriteListItem("item"));
        Assert.True(orch.WriteCodeStart("csharp"));
        Assert.True(orch.WriteCodeEnd());
        Assert.True(orch.WriteCallout(CalloutSeverity.Note, "msg"));
        Assert.True(orch.WriteRule());
        Assert.True(orch.WriteBreakdown([new Breakdown("test", [new Slice("a", 1)])]));
    }

    // ── Cascade field → streaming table ──

    [Fact]
    public void FieldCascade_StreamingOnlyFormatter_RendersFieldsViaStreaming()
    {
        var orch = MarkoutWriter.Create(new StreamingFormatter());

        var result = orch.WriteFields(new MarkoutField("Key", "Val"));

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("[BEGIN]", output);
        Assert.Contains("Key|Val", output);
        Assert.Contains("[END:0]", output);
    }

    // ── Helpers ──

    /// <summary>
    /// A formatter that only implements ITableFormatter — no IFieldFormatter.
    /// Used to test field-to-table cascade dispatch.
    /// </summary>
    private class TableOnlyFormatter : IMarkoutFormatter, ITableFormatter
    {
        void ITableFormatter.FormatTable(TextWriter writer, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
        {
            writer.Write(string.Join(" | ", headers));
            writer.WriteLine();
            foreach (var row in rows)
            {
                writer.Write(string.Join(" | ", row));
                writer.WriteLine();
            }
        }
    }

    /// <summary>
    /// A formatter that only implements IStreamingTableFormatter — no batch ITableFormatter.
    /// Used to test streaming table dispatch and field→streaming cascade.
    /// </summary>
    private class StreamingFormatter : IMarkoutFormatter, IStreamingTableFormatter
    {
        void IStreamingTableFormatter.BeginTable(TextWriter writer, ReadOnlySpan<string> headers, MarkoutWriterOptions options)
        {
            writer.Write("[BEGIN]");
            writer.Write(string.Join("|", headers));
            writer.WriteLine();
        }

        void IStreamingTableFormatter.WriteRow(TextWriter writer, ReadOnlySpan<string> values)
        {
            writer.Write(string.Join("|", values));
            writer.WriteLine();
        }

        void IStreamingTableFormatter.EndTable(TextWriter writer, int skippedRows)
        {
            writer.Write($"[END:{skippedRows}]");
            writer.WriteLine();
        }
    }

    /// <summary>
    /// ToString() previews only what has reached the target, so an open table whose rows the
    /// formatter is buffering is absent from the preview. Complete() is what renders it.
    /// </summary>
    /// <remarks>
    /// This is documented rather than fixed because it is not a regression: the committing
    /// ToString() of 0.35.1 dropped the same rows, for the same reason -- neither closes the open
    /// table, and a buffering formatter has written nothing until the table closes. Rendering a
    /// buffered table without ending it needs a non-mutating render path that does not exist, which
    /// is its own change and not a release fix. The gate exists so that the documented boundary is
    /// the tested one, and so that adding that path is a visible test change rather than a silent
    /// one.
    /// </remarks>
    [Fact]
    public void ToString_WithOpenBufferedTable_OmitsItAndCompleteRendersIt()
    {
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
        var writer = MarkoutWriter.Create(new TableFormatter(), options);

        writer.WriteTableStart(["Col1"]);
        writer.WriteTableRow(["Value1"]);

        Assert.DoesNotContain("Value1", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("Value1", writer.Complete(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The truncation footer that follows a shortened table is library-generated text, so it has
    /// to use the writer's terminator like every other line the library emits.
    /// </summary>
    /// <remarks>
    /// It was previously written as <c>WriteLine("\n... and N more")</c> at six sites across the
    /// three formatters. The embedded newline is a literal LF whatever the writer is set to, so a
    /// CRLF <see cref="MarkoutWriterOptions.NewLine"/> produced a document with mixed endings --
    /// and nothing noticed, because the existing NewLine tests serialize a record that is never
    /// truncated. A sentinel terminator is used so that any surviving literal newline is visible
    /// on every platform, rather than only where Environment.NewLine is CRLF.
    /// </remarks>
    [Theory]
    [InlineData("markdown")]
    [InlineData("table")]
    [InlineData("plaintext")]
    public void TruncationFooter_UsesConfiguredNewLine(string formatterName)
    {
        IMarkoutFormatter formatter = formatterName switch
        {
            "markdown" => new MarkdownFormatter(),
            "table" => new TableFormatter(),
            _ => new PlainTextFormatter(),
        };
        var options = new MarkoutWriterOptions { NewLine = "<NL>", MaxItems = 2 };
        var writer = new MarkoutWriter(formatter, options);

        writer.WriteTable(["A"], [["1"], ["2"], ["3"], ["4"]]);
        var output = writer.Complete();

        Assert.Contains("... and 2 more", output, StringComparison.Ordinal);
        Assert.Contains("<NL>", output, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', output);
        Assert.DoesNotContain('\r', output);
    }

    /// <summary>
    /// A minimal formatter that only implements IMarkoutFormatter — no capabilities.
    /// Used to test that all shapes return false.
    /// </summary>
    private class MinimalFormatter : IMarkoutFormatter { }
}
