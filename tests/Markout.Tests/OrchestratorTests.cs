using Markout;
using Markout.Formatting;

namespace Markout.Tests;

public class OrchestratorTests
{
    // ── MarkdownWriter formatter — all shapes render ──

    [Fact]
    public void MarkdownWriter_WriteHeading_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteHeading(1, "Title");
        Assert.True(result);
        Assert.Equal("# Title", orch.ToString());
    }

    [Fact]
    public void MarkdownWriter_WriteHeading_WithContext()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteHeading(2, "Section", "v1.0");
        Assert.True(result);
        Assert.Equal("## Section (v1.0)", orch.ToString());
    }

    [Fact]
    public void WriteHeading_InvalidLevel_Throws()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        Assert.Throws<ArgumentOutOfRangeException>(() => orch.WriteHeading(0, "Bad"));
        Assert.Throws<ArgumentOutOfRangeException>(() => orch.WriteHeading(7, "Bad"));
    }

    [Fact]
    public void MarkdownWriter_WriteFields_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteFields(new MarkoutField("Name", "Alice"), new MarkoutField("Age", "30"));
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Name: Alice", output);
        Assert.Contains("Age: 30", output);
    }

    [Fact]
    public void MarkdownWriter_WriteFields_Bold()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), new MarkoutWriterOptions { BoldFieldNames = true });
        orch.WriteFields(new MarkoutField("Name", "Alice"));
        var output = orch.ToString();
        Assert.Contains("**Name:** Alice", output);
    }

    [Fact]
    public void MarkdownWriter_WriteTable_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteTable(["Name", "Age"], [["Alice", "30"], ["Bob", "25"]]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("| Name |", output);
        Assert.Contains("| Alice |", output);
    }

    [Fact]
    public void MarkdownWriter_WriteCodeBlock_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        Assert.True(orch.WriteCodeStart("csharp"));
        orch.WriteBlankLine(); // just to have some content
        Assert.True(orch.WriteCodeEnd());
        var output = orch.ToString();
        Assert.Contains("```csharp", output);
        Assert.Contains("```", output);
    }

    [Fact]
    public void MarkdownWriter_WriteCallout_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteCallout(CalloutSeverity.Warning, "Watch out!");
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("[!WARNING]", output);
        Assert.Contains("Watch out!", output);
    }

    [Fact]
    public void MarkdownWriter_WriteQuotation_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteQuotation("To be or not to be");
        Assert.True(result);
        Assert.Contains("> To be or not to be", orch.ToString());
    }

    [Fact]
    public void MarkdownWriter_WriteRule_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteParagraph("Before");
        var result = orch.WriteRule();
        Assert.True(result);
        Assert.Contains("---", orch.ToString());
    }

    [Fact]
    public void MarkdownWriter_WriteDescriptions_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteDescriptions([new Description("Term", "Definition")]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("**Term:**", output);
        Assert.Contains("Definition", output);
    }

    [Fact]
    public void MarkdownWriter_WriteBreakdown_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var breakdown = new Breakdown("Test", [new Segment("Pass", 8), new Segment("Fail", 2)]);
        var result = orch.WriteBreakdown([breakdown]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Category", output);
        Assert.Contains("Pass", output);
    }

    [Fact]
    public void MarkdownWriter_WriteMetrics_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteMetrics([new Metric("CPU", 75), new Metric("Mem", 50)]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Label", output);
        Assert.Contains("CPU", output);
    }

    [Fact]
    public void MarkdownWriter_WriteListItem_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteListItem("Item one");
        Assert.True(result);
        Assert.Contains("- Item one", orch.ToString());
    }

    [Fact]
    public void MarkdownWriter_WriteList_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteList("A", "B", "C");
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("- A", output);
        Assert.Contains("- B", output);
        Assert.Contains("- C", output);
    }

    [Fact]
    public void MarkdownWriter_WriteArray_WithKey_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteArray("Items", "X", "Y");
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Items:", output);
        Assert.Contains("- X", output);
    }

    [Fact]
    public void MarkdownWriter_WriteParagraph_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteParagraph("Hello world");
        Assert.True(result);
        Assert.Equal("Hello world", orch.ToString());
    }

    [Fact]
    public void MarkdownWriter_WriteTree_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteTree(new TreeNode("Root", null, new TreeNode("Child")));
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Child", output);
    }

    [Fact]
    public void MarkdownWriter_WriteTreeNode_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteTreeNode("node text", ">> ");
        Assert.True(result);
        Assert.Contains(">> node text", orch.ToString());
    }

    // ── OneLineWriter formatter — subset renders ──

    [Fact]
    public void OneLineWriter_WriteHeading_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteHeading(1, "Title");
        Assert.False(result);
    }

    [Fact]
    public void OneLineWriter_WriteTable_ReturnsTrue()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteTable(["Col"], [["Val"]]);
        Assert.True(result);
    }

    [Fact]
    public void OneLineWriter_WriteFields_ReturnsTrue()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteFields(new MarkoutField("K", "V"));
        Assert.True(result);
    }

    [Fact]
    public void OneLineWriter_WriteCallout_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteCallout(CalloutSeverity.Note, "msg");
        Assert.False(result);
    }

    [Fact]
    public void OneLineWriter_WriteCodeStart_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteCodeStart("csharp");
        Assert.False(result);
    }

    [Fact]
    public void OneLineWriter_WriteBreakdown_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteBreakdown([new Breakdown("X", [new Segment("A", 1)])]);
        Assert.False(result);
    }

    [Fact]
    public void OneLineWriter_WriteRule_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new OneLineWriter(TextWriter.Null));
        var result = orch.WriteRule();
        Assert.False(result);
    }

    // ── Section filtering ──

    [Fact]
    public void IncludeSections_FiltersContent()
    {
        var options = new MarkoutWriterOptions { IncludeSections = ["Visible"] };
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

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
    public void ExcludeSections_FiltersContent()
    {
        var options = new MarkoutWriterOptions { ExcludeSections = ["Hidden"] };
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

        orch.WriteHeading(2, "Visible");
        orch.WriteParagraph("Included");
        orch.WriteHeading(2, "Hidden");
        orch.WriteParagraph("Excluded");

        var output = orch.ToString();
        Assert.Contains("Included", output);
        Assert.DoesNotContain("Excluded", output);
    }

    [Fact]
    public void BothIncludeAndExcludeSections_Throws()
    {
        var options = new MarkoutWriterOptions
        {
            IncludeSections = ["A"],
            ExcludeSections = ["B"]
        };
        Assert.Throws<InvalidOperationException>(() =>
            MarkoutOrchestrator.Create(new MarkdownWriter(), options));
    }

    [Fact]
    public void EmptyIncludeSections_PreambleOnly()
    {
        var options = new MarkoutWriterOptions { IncludeSections = [] };
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);
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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);
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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);
        orch.WriteTable(["Name", "Age"], [["Alice", "30"]]);

        var output = orch.ToString();
        Assert.Contains("Name", output);
        Assert.DoesNotContain("Age", output);
    }

    // ── Field-to-table cascade ──

    [Fact]
    public void FieldCascade_TableOnlyFormatter_RendersFieldsAsTable()
    {
        var orch = MarkoutOrchestrator.Create(new TableOnlyFormatter());

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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());

        orch.WriteFields(new MarkoutField("A", "1"));

        var output = orch.ToString();
        // MarkdownWriter implements IFieldFormatter, so fields render as key: value, not as a table
        Assert.Contains("A:", output);
        Assert.DoesNotContain("| Field |", output);
    }

    [Fact]
    public void FieldCascade_WriteFieldsInline_FallsBackToTable()
    {
        var orch = MarkoutOrchestrator.Create(new TableOnlyFormatter());

        orch.WriteFieldsInline(new MarkoutField("A", "1"), new MarkoutField("B", "2"));

        var output = orch.ToString();
        // No IFieldFormatter, so inline falls back to table
        Assert.Contains("Field", output);
        Assert.Contains("A", output);
    }

    [Fact]
    public void FieldCascade_WriteFieldsBulleted_FallsBackToTable()
    {
        var orch = MarkoutOrchestrator.Create(new TableOnlyFormatter());

        orch.WriteFieldsBulleted(new MarkoutField("X", "Y"));

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("X", output);
    }

    [Fact]
    public void FieldCascade_WriteFieldsNumbered_FallsBackToTable()
    {
        var orch = MarkoutOrchestrator.Create(new TableOnlyFormatter());

        orch.WriteFieldsNumbered(new MarkoutField("X", "Y"));

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("X", output);
    }

    [Fact]
    public void FieldCascade_WriteField_FallsBackToTable()
    {
        var orch = MarkoutOrchestrator.Create(new TableOnlyFormatter());

        orch.WriteField("Key", "Val");

        var output = orch.ToString();
        Assert.Contains("Field", output);
        Assert.Contains("Key", output);
    }

    [Fact]
    public void FieldCascade_MinimalFormatter_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new MinimalFormatter());

        var result = orch.WriteFields(new MarkoutField("A", "1"));

        Assert.False(result);
    }

    // ── MaxItems ──

    [Fact]
    public void MaxItems_LimitsTableRows()
    {
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

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
            Projection = MarkoutProjection.WithSections("Data")
        };
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

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
            Projection = MarkoutProjection.WithSections("Data"),
            ExcludeSections = ["Other"]
        };
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

        orch.WriteSectionStart(2, "Data");
        // No content written — heading should not appear
        orch.WriteSectionEnd();

        var output = orch.ToString();
        Assert.Equal("", output);
    }

    // ── Output parity with MarkdownWriter ──

    [Fact]
    public void OutputParity_Heading()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(1, "Title");
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteHeading(1, "Title");
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_Fields()
    {
        var writer = new MarkdownWriter();
        writer.WriteFields(new MarkoutField("A", "1"), new MarkoutField("B", "2"));
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteFields(new MarkoutField("A", "1"), new MarkoutField("B", "2"));
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_Table()
    {
        var writer = new MarkdownWriter();
        writer.WriteTable(["Name", "Age"], [["Alice", "30"]]);
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteTable(["Name", "Age"], [["Alice", "30"]]);
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_HeadingThenFields()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(1, "Title");
        writer.WriteFields(new MarkoutField("K", "V"));
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteHeading(1, "Title");
        orch.WriteFields(new MarkoutField("K", "V"));
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_HeadingThenTable()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(2, "Data");
        writer.WriteTable(["X"], [["1"]]);
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteHeading(2, "Data");
        orch.WriteTable(["X"], [["1"]]);
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_ListItems()
    {
        var writer = new MarkdownWriter();
        writer.WriteList("A", "B");
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteList("A", "B");
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_CodeBlock()
    {
        var writer = new MarkdownWriter();
        writer.WriteCodeStart("json");
        writer.WriteCodeEnd();
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteCodeStart("json");
        orch.WriteCodeEnd();
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutputParity_Callout()
    {
        var writer = new MarkdownWriter();
        writer.WriteCallout(CalloutSeverity.Note, "Info");
        var expected = writer.ToString();

        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteCallout(CalloutSeverity.Note, "Info");
        var actual = orch.ToString();

        Assert.Equal(expected, actual);
    }

    // ── ToString ──

    [Fact]
    public void ToString_TrimsTrailingWhitespace()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteParagraph("Text");
        orch.WriteBlankLine();

        var output = orch.ToString();
        Assert.Equal("Text", output);
    }

    [Fact]
    public void ToString_RendersFieldOutput()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());

        orch.WriteFields(new MarkoutField("Key", "Val"));

        var output = orch.ToString();
        Assert.Contains("Key", output);
        Assert.Contains("Val", output);
    }

    // ── Static factory ──

    [Fact]
    public void Create_WithTextWriter_TypeInference()
    {
        var sw = new StringWriter();
        var orch = MarkoutOrchestrator.Create(sw, new MarkdownWriter());
        orch.WriteHeading(1, "Test");
        Assert.Contains("# Test", sw.ToString());
    }

    [Fact]
    public void Create_InMemory_TypeInference()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteParagraph("Hello");
        Assert.Equal("Hello", orch.ToString());
    }

    [Fact]
    public void ExplicitGeneric_Works()
    {
        var orch = new MarkoutOrchestrator<MarkdownWriter>(new MarkdownWriter());
        orch.WriteHeading(1, "Explicit");
        Assert.Contains("# Explicit", orch.ToString());
    }

    // ── Streaming table ──

    [Fact]
    public void StreamingTable_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
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
        var orch = MarkoutOrchestrator.Create(new MinimalFormatter());
        var result = orch.WriteTableStart("Col");
        Assert.False(result);
    }

    [Fact]
    public void WriteTableRow_WithoutStart_Throws()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        Assert.Throws<InvalidOperationException>(() => orch.WriteTableRow("value"));
    }

    [Fact]
    public void WriteCodeEnd_WithoutStart_Throws()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        Assert.Throws<InvalidOperationException>(() => orch.WriteCodeEnd());
    }

    [Fact]
    public void WriteCodeStart_Nested_Throws()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        orch.WriteCodeStart();
        Assert.Throws<InvalidOperationException>(() => orch.WriteCodeStart());
    }

    // ── Minimal formatter (implements only IMarkoutFormatter) ──

    [Fact]
    public void MinimalFormatter_AllShapesReturnFalse()
    {
        var orch = MarkoutOrchestrator.Create(new MinimalFormatter());

        Assert.False(orch.WriteHeading(1, "Title"));
        Assert.False(orch.WriteFields(new MarkoutField("K", "V")));
        Assert.False(orch.WriteTable(["H"], [["V"]]));
        Assert.False(orch.WriteListItem("item"));
        Assert.False(orch.WriteCallout(CalloutSeverity.Note, "msg"));
        Assert.False(orch.WriteRule());
        Assert.False(orch.WriteBreakdown([new Breakdown("X", [new Segment("A", 1)])]));
        Assert.False(orch.WriteMetrics([new Metric("M", 1)]));
    }

    [Fact]
    public void MinimalFormatter_UniversalShapesReturnTrue()
    {
        var orch = MarkoutOrchestrator.Create(new MinimalFormatter());

        Assert.True(orch.WriteParagraph("text"));
        Assert.True(orch.WriteTreeNode("node"));
        Assert.True(orch.WriteTree(new TreeNode("root")));
    }

    // ── WriteFieldsTable ──

    [Fact]
    public void WriteFieldsTable_Renders()
    {
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteFieldsTable(new MarkoutField("Name", "Alice"));
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("| Field |", output);
        Assert.Contains("| Name |", output);
    }

    [Fact]
    public void WriteFieldsTable_UnsupportedFormatter_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new MinimalFormatter());
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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter(), options);

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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());
        var result = orch.WriteVerticalMetrics([new Metric("A", 10), new Metric("B", 5)]);
        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Label", output);
    }

    [Fact]
    public void WriteVerticalMetrics_UnsupportedFormatter_ReturnsFalse()
    {
        var orch = MarkoutOrchestrator.Create(new MinimalFormatter());
        var result = orch.WriteVerticalMetrics([new Metric("A", 10)]);
        Assert.False(result);
    }

    // ── Flush ──

    [Fact]
    public void Flush_WritesOutputToStream()
    {
        var sw = new StringWriter();
        var orch = MarkoutOrchestrator.Create(sw, new MarkdownWriter());

        orch.WriteFields(new MarkoutField("K", "V"));
        orch.Flush();

        var output = sw.ToString();
        Assert.Contains("K", output);
    }

    // ── Streaming table via IStreamingTableFormatter ──

    [Fact]
    public void StreamingTable_DirectStreaming_WritesRowsImmediately()
    {
        var orch = MarkoutOrchestrator.Create(new StreamingFormatter());

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
        var orch = MarkoutOrchestrator.Create(new StreamingFormatter(), options);

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
        var orch = MarkoutOrchestrator.Create(new StreamingFormatter());

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
        var orch = MarkoutOrchestrator.Create(new MarkdownWriter());

        // IList<string[]> should take the batch path
        orch.WriteTable(["Name"], new List<string[]> { new[] { "Alice" } });

        var output = orch.ToString();
        Assert.Contains("| Name |", output);
        Assert.Contains("| Alice |", output);
    }

    // ── IDocumentFormatter aggregate ──

    [Fact]
    public void DocumentFormatter_MarkdownWriter_ImplementsAggregate()
    {
        var writer = new MarkdownWriter();
        Assert.IsAssignableFrom<IDocumentFormatter>(writer);
    }

    [Fact]
    public void DocumentFormatter_WorksAsOrchestratorConstraint()
    {
        // Verify MarkdownWriter can be used via IDocumentFormatter
        var writer = new MarkdownWriter();
        IDocumentFormatter df = writer;

        var sw = new StringWriter();
        df.FormatHeading(sw, 1, "Title", null);
        Assert.Contains("# Title", sw.ToString());
    }

    // ── UnicodeWriter as orchestrator formatter ──

    [Fact]
    public void UnicodeWriter_Orchestrator_WritesHeading()
    {
        var orch = MarkoutOrchestrator.Create(new UnicodeWriter(TextWriter.Null));

        var result = orch.WriteHeading(1, "Test");

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Test", output);
    }

    [Fact]
    public void UnicodeWriter_Orchestrator_WritesTable()
    {
        var orch = MarkoutOrchestrator.Create(new UnicodeWriter(TextWriter.Null));

        var result = orch.WriteTable(["Name", "Age"], [["Alice", "30"]]);

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("NAME", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void UnicodeWriter_Orchestrator_WritesFields()
    {
        var orch = MarkoutOrchestrator.Create(new UnicodeWriter(TextWriter.Null));

        var result = orch.WriteFields(new MarkoutField("Status", "OK"));

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Status", output);
        Assert.Contains("OK", output);
    }

    [Fact]
    public void UnicodeWriter_Orchestrator_WritesCallout()
    {
        var orch = MarkoutOrchestrator.Create(new UnicodeWriter(TextWriter.Null));

        var result = orch.WriteCallout(CalloutSeverity.Warning, "Watch out!");

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("Watch out!", output);
    }

    [Fact]
    public void UnicodeWriter_Orchestrator_WritesMetrics()
    {
        var orch = MarkoutOrchestrator.Create(new UnicodeWriter(TextWriter.Null));

        var result = orch.WriteMetrics([new Metric("CPU", 75)]);

        Assert.True(result);
        var output = orch.ToString();
        Assert.Contains("CPU", output);
    }

    [Fact]
    public void UnicodeWriter_Orchestrator_AllShapesReturn_True()
    {
        var orch = MarkoutOrchestrator.Create(new UnicodeWriter(TextWriter.Null));

        Assert.True(orch.WriteHeading(1, "H1"));
        Assert.True(orch.WriteFields(new MarkoutField("K", "V")));
        Assert.True(orch.WriteTable(["H"], [["V"]]));
        Assert.True(orch.WriteListItem("item"));
        Assert.True(orch.WriteCodeStart("csharp"));
        Assert.True(orch.WriteCodeEnd());
        Assert.True(orch.WriteCallout(CalloutSeverity.Note, "msg"));
        Assert.True(orch.WriteRule());
        Assert.True(orch.WriteBreakdown([new Breakdown("test", [new Segment("a", 1)])]));
    }

    // ── Cascade field → streaming table ──

    [Fact]
    public void FieldCascade_StreamingOnlyFormatter_RendersFieldsViaStreaming()
    {
        var orch = MarkoutOrchestrator.Create(new StreamingFormatter());

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
        void ITableFormatter.FormatTable(TextWriter writer, string[] headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
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
        void IStreamingTableFormatter.BeginTable(TextWriter writer, string[] headers, MarkoutWriterOptions options)
        {
            writer.Write("[BEGIN]");
            writer.Write(string.Join("|", headers));
            writer.WriteLine();
        }

        void IStreamingTableFormatter.WriteRow(TextWriter writer, string[] values)
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
    /// A minimal formatter that only implements IMarkoutFormatter — no capabilities.
    /// Used to test that all shapes return false.
    /// </summary>
    private class MinimalFormatter : IMarkoutFormatter { }
}
