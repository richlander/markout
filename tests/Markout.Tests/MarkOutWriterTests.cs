using Markout;

namespace Markout.Tests;

public class MarkoutWriterTests
{
    [Fact]
    public void WriteHeading_Level1_WritesCorrectMarkdown()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(1, "Package");

        Assert.Equal("# Package", writer.ToString());
    }

    [Fact]
    public void WriteHeading_Level2_WritesCorrectMarkdown()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(2, "Dependencies");

        Assert.Equal("## Dependencies", writer.ToString());
    }

    [Fact]
    public void WriteHeading_WithContext_WritesCorrectMarkdown()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(2, "Dependencies", "net6.0");

        Assert.Equal("## Dependencies (net6.0)", writer.ToString());
    }

    [Fact]
    public void WriteField_WritesKeyValue()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Name", "Alice");

        Assert.Equal("Name: Alice", writer.ToString());
    }

    [Fact]
    public void WriteField_MultipleCallsWriteMultipleLines()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Name", "Alice");
        writer.WriteField("Age", "30");

        Assert.Equal("Name: Alice\nAge: 30", writer.ToString());
    }

    [Fact]
    public void WriteField_InForeachLoop()
    {
        var writer = new MarkoutWriter();
        var data = new Dictionary<string, string>
        {
            ["Name"] = "Alice",
            ["Role"] = "Developer",
            ["Team"] = "Platform"
        };

        foreach (var kvp in data)
            writer.WriteField(kvp.Key, kvp.Value);

        var output = writer.ToString();
        Assert.Contains("Name: Alice", output);
        Assert.Contains("Role: Developer", output);
        Assert.Contains("Team: Platform", output);
    }

    [Fact]
    public void WriteFields_String_WritesKeyValue()
    {
        var writer = new MarkoutWriter();
        writer.WriteFields([new("Name", "Newtonsoft.Json")]);

        Assert.Equal("Name: Newtonsoft.Json", writer.ToString());
    }

    [Fact]
    public void WriteFields_BooleanTrue_WritesYes()
    {
        var writer = new MarkoutWriter();
        writer.WriteFields([new("Signed", "yes")]);

        Assert.Equal("Signed: yes", writer.ToString());
    }

    [Fact]
    public void WriteFields_BooleanFalse_WritesNo()
    {
        var writer = new MarkoutWriter();
        writer.WriteFields([new("Signed", "no")]);

        Assert.Equal("Signed: no", writer.ToString());
    }

    [Fact]
    public void WriteFields_Integer_WritesNumber()
    {
        var writer = new MarkoutWriter();
        writer.WriteFields([new("Count", "42")]);

        Assert.Equal("Count: 42", writer.ToString());
    }

    [Fact]
    public void WriteFields_Double_WritesNumber()
    {
        var writer = new MarkoutWriter();
        writer.WriteFields([new("Version", "3.14")]);

        Assert.Equal("Version: 3.14", writer.ToString());
    }

    [Fact]
    public void WriteArray_StringItems_WritesListSyntax()
    {
        var writer = new MarkoutWriter();
        writer.WriteArray("Frameworks", new[] { "netstandard2.0", "net6.0", "net8.0" });

        var expected = """
            Frameworks:
            - netstandard2.0
            - net6.0
            - net8.0
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteArray_EmptyArray_WritesKeyOnly()
    {
        var writer = new MarkoutWriter();
        writer.WriteArray("Frameworks", Array.Empty<string>());

        Assert.Equal("Frameworks:", writer.ToString());
    }

    [Fact]
    public void WriteTable_WritesMarkdownTable()
    {
        var writer = new MarkdownWriter();
        writer.WriteTableStart("File", "Arch", "Signed");
        writer.WriteTableRow("Foo.dll", "AnyCPU", "yes");
        writer.WriteTableRow("Bar.dll", "x64", "no");
        writer.WriteTableEnd();

        var expected = """
            | File | Arch | Signed |
            | ---- | ---- | ------ |
            | Foo.dll | AnyCPU | yes |
            | Bar.dll | x64 | no |
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteTable_PrettyTables_PadsColumns()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { PrettyTables = true });
        writer.WriteTableStart("File", "Arch", "Signed");
        writer.WriteTableRow("Foo.dll", "AnyCPU", "yes");
        writer.WriteTableRow("Bar.dll", "x64", "no");
        writer.WriteTableEnd();

        var expected = """
            | File    | Arch   | Signed |
            | ------- | ------ | ------ |
            | Foo.dll | AnyCPU | yes    |
            | Bar.dll | x64    | no     |
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteTable_Batch_PrettyTables_PadsColumns()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { PrettyTables = true });
        writer.WriteTable(
            ["Name", "Born"],
            [["Alice", "1990"], ["Bob", "1985"]]);

        var expected = """
            | Name  | Born |
            | ----- | ---- |
            | Alice | 1990 |
            | Bob   | 1985 |
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void MultipleElements_AddsBlankLinesBetweenSections()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(1, "Package");
        writer.WriteFields(
            new("Name", "Test"),
            new("Version", "1.0.0"));
        writer.WriteHeading(2, "Dependencies");
        writer.WriteFields([new("Count", "5")]);

        var expected = "# Package\n\nName: Test  \nVersion: 1.0.0  \n\n## Dependencies\n\nCount: 5";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteDateTime_WritesIso8601Format()
    {
        var writer = new MarkoutWriter();
        var date = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        writer.WriteFields([new("Published", date.ToString("O"))]);

        Assert.StartsWith("Published: 2024-01-15T10:30:00", writer.ToString());
    }

    [Fact]
    public void IncludeSections_FiltersToSpecifiedSections()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { IncludeSections = ["First"] });

        writer.WriteHeading(1, "Title");
        writer.WriteParagraph("Intro");
        writer.WriteHeading(2, "First");    // included
        writer.WriteFields([new("A", "1")]);
        writer.WriteHeading(2, "Second");   // excluded
        writer.WriteFields([new("B", "2")]);
        writer.WriteHeading(2, "Third");    // excluded
        writer.WriteFields([new("C", "3")]);

        var expected = "# Title\n\nIntro\n\n## First\n\nA: 1";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void ExcludeSections_SkipsSpecifiedSections()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { ExcludeSections = ["Second"] });

        writer.WriteHeading(1, "Title");
        writer.WriteHeading(2, "First");    // included
        writer.WriteFields([new("A", "1")]);
        writer.WriteHeading(2, "Second");   // excluded
        writer.WriteFields([new("B", "2")]);
        writer.WriteHeading(2, "Third");    // included
        writer.WriteFields([new("C", "3")]);

        var expected = "# Title\n\n## First\n\nA: 1  \n\n## Third\n\nC: 3";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void SectionFiltering_ContentBeforeFirstH2_AlwaysIncluded()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { IncludeSections = ["Second"] });

        writer.WriteHeading(1, "Title");
        writer.WriteParagraph("This is before any H2");
        writer.WriteHeading(2, "First");    // excluded
        writer.WriteFields([new("A", "1")]);
        writer.WriteHeading(2, "Second");   // included
        writer.WriteFields([new("B", "2")]);

        var expected = "# Title\n\nThis is before any H2\n\n## Second\n\nB: 2";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void SectionFiltering_TableSpanningExcludedSection_NotWritten()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { ExcludeSections = ["Data"] });

        writer.WriteHeading(1, "Title");
        writer.WriteHeading(2, "Data");     // excluded
        writer.WriteTableStart("Name", "Value");
        writer.WriteTableRow("Foo", "Bar");
        writer.WriteTableEnd();
        writer.WriteHeading(2, "Other");    // included
        writer.WriteFields([new("X", "Y")]);

        var expected = "# Title\n\n## Other\n\nX: Y";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteFieldsInline_SingleField_WritesSinglePair()
    {
        var writer = new MarkoutWriter();
        writer.WriteFieldsInline([new("Type", "Library")]);

        Assert.Equal("Type: Library", writer.ToString());
    }

    [Fact]
    public void WriteFieldsInline_MultipleFields_WritesPipeSeparated()
    {
        var writer = new MarkoutWriter();
        writer.WriteFieldsInline(
            new MarkoutField("Type", "Library"),
            new MarkoutField("TFM", "net8.0"),
            new MarkoutField("Updated", "2026-01-15"));

        Assert.Equal("Type: Library | TFM: net8.0 | Updated: 2026-01-15", writer.ToString());
    }

    [Fact]
    public void WriteFieldsInline_EmptyFields_WritesNothing()
    {
        var writer = new MarkoutWriter();
        writer.WriteFieldsInline();

        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void WriteFieldsInline_EmptyValue_WritesEmptyString()
    {
        var writer = new MarkoutWriter();
        writer.WriteFieldsInline(
            new("Status", ""),
            new("Count", "5"));

        Assert.Equal("Status:  | Count: 5", writer.ToString());
    }

    [Fact]
    public void WriteFieldsInline_AfterHeading_AddsBlankLine()
    {
        var writer = new MarkdownWriter();
        writer.WriteHeading(1, "Package 1.0.0");
        writer.WriteFieldsInline(
            new MarkoutField("Type", "Library"),
            new MarkoutField("TFM", "net8.0"));

        var expected = "# Package 1.0.0\n\nType: Library | TFM: net8.0";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteTree_SimpleNodes_RendersTreeStructure()
    {
        var writer = new MarkoutWriter();
        writer.WriteTree(new TreeNode("Root", null, new TreeNode("Child1"), new TreeNode("Child2")));

        var output = writer.ToString();
        Assert.Contains("└─ Root", output);
        Assert.Contains("├─ Child1", output);
        Assert.Contains("└─ Child2", output);
    }

    [Fact]
    public void WriteTree_WithIcons_RendersIconBeforeLabel()
    {
        var writer = new MarkoutWriter();
        writer.WriteTree(
            new("local.dll", "📁"),
            new("platform.dll", "🚢"),
            new("unknown.dll", "❓"));

        var output = writer.ToString();
        Assert.Contains("📁 local.dll", output);
        Assert.Contains("🚢 platform.dll", output);
        Assert.Contains("❓ unknown.dll", output);
    }

    [Fact]
    public void WriteTree_WithNestedIcons_RendersAllIcons()
    {
        var writer = new MarkoutWriter();
        writer.WriteTree(new TreeNode("lib", "📁", new TreeNode("net8.0", "📂", new TreeNode("MyLib.dll"))));

        var output = writer.ToString();
        Assert.Contains("📁 lib", output);
        Assert.Contains("📂 net8.0", output);
        Assert.Contains("MyLib.dll", output);
        // Child without icon should not have icon prefix
        Assert.Contains("└─ MyLib.dll", output);
    }

    [Fact]
    public void WriteTree_WithIncludeBadgesFalse_OmitsBadges()
    {
        var options = new MarkoutWriterOptions { IncludeBadges = false };
        var writer = new MarkoutWriter(options);
        writer.WriteTree(
            new("local.dll", "📁"),
            new("platform.dll", "🚢"));

        var output = writer.ToString();
        // Icons should not appear
        Assert.DoesNotContain("📁", output);
        Assert.DoesNotContain("🚢", output);
        // Labels should still appear
        Assert.Contains("local.dll", output);
        Assert.Contains("platform.dll", output);
    }

    [Fact]
    public void MarkoutWriterOptions_Default_HasExpectedValues()
    {
        var options = new MarkoutWriterOptions();

        Assert.True(options.IncludeBadges);
        Assert.True(options.IncludeDescription);
        Assert.False(options.BoldFieldNames);
        Assert.Null(options.IncludeSections);
        Assert.Null(options.ExcludeSections);
    }

    [Fact]
    public void MarkoutWriterOptions_Default_IsReadOnly()
    {
        Assert.True(MarkoutWriterOptions.Default.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => MarkoutWriterOptions.Default.BoldFieldNames = true);
    }

    [Fact]
    public void MarkoutWriterOptions_MakeReadOnly_PreventsModification()
    {
        var options = new MarkoutWriterOptions { BoldFieldNames = true };
        options.MakeReadOnly();

        Assert.Throws<InvalidOperationException>(() => options.BoldFieldNames = false);
        Assert.Throws<InvalidOperationException>(() => options.IncludeBadges = false);
        Assert.Throws<InvalidOperationException>(() => options.IncludeDescription = false);
        Assert.Throws<InvalidOperationException>(() => options.IncludeSections = ["First"]);
        Assert.Throws<InvalidOperationException>(() => options.ExcludeSections = ["Second"]);
    }

    [Fact]
    public void MarkoutWriter_BothIncludeAndExclude_ThrowsInvalidOperation()
    {
        var options = new MarkoutWriterOptions
        {
            IncludeSections = ["First"],
            ExcludeSections = ["Second"]
        };

        Assert.Throws<InvalidOperationException>(() => new MarkoutWriter(options));
    }

    [Fact]
    public void IncludeSections_EmptySet_IncludesPreambleOnly()
    {
        var writer = new MarkdownWriter(new MarkoutWriterOptions { IncludeSections = [] });

        writer.WriteHeading(1, "Title");
        writer.WriteParagraph("Preamble text");
        writer.WriteHeading(2, "First");
        writer.WriteFields([new("A", "1")]);

        var expected = "# Title\n\nPreamble text";
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void CurrentContext_DefaultIsBlock()
    {
        var writer = new MarkoutWriter();
        Assert.Equal(MarkoutRenderContext.Block, writer.CurrentContext);
    }

    [Fact]
    public void CurrentContext_InTable_ReturnsTable()
    {
        var writer = new MarkoutWriter();
        writer.WriteTableStart("A", "B");
        Assert.Equal(MarkoutRenderContext.Table, writer.CurrentContext);
        writer.WriteTableEnd();
        Assert.Equal(MarkoutRenderContext.Block, writer.CurrentContext);
    }

    [Fact]
    public void CurrentContext_InCodeBlock_ReturnsCodeBlock()
    {
        var writer = new MarkoutWriter();
        writer.WriteCodeStart("csharp");
        Assert.Equal(MarkoutRenderContext.CodeBlock, writer.CurrentContext);
        writer.WriteCodeEnd();
        Assert.Equal(MarkoutRenderContext.Block, writer.CurrentContext);
    }

    [Fact]
    public void WriteCodeStart_Nested_Throws()
    {
        var writer = new MarkoutWriter();
        writer.WriteCodeStart();
        Assert.Throws<InvalidOperationException>(() => writer.WriteCodeStart());
    }

    [Fact]
    public void WriteCodeEnd_WithoutStart_Throws()
    {
        var writer = new MarkoutWriter();
        Assert.Throws<InvalidOperationException>(() => writer.WriteCodeEnd());
    }

    [Fact]
    public void WriteTableStart_InCodeBlock_Throws()
    {
        var writer = new MarkoutWriter();
        writer.WriteCodeStart();
        Assert.Throws<InvalidOperationException>(() => writer.WriteTableStart("A"));
    }

    [Fact]
    public void WriteDescriptions_PlainText_WritesLabelAndDescription()
    {
        var writer = new MarkoutWriter();
        writer.WriteDescriptions([
            new Description("Insight", "What does the generic math hierarchy look like?"),
            new Description("Discovery", "What can JsonSerializer do?"),
        ]);

        var expected = """
            - Insight: What does the generic math hierarchy look like?
            - Discovery: What can JsonSerializer do?
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteDescriptions_PlainText_WithDetail_IndentsDetail()
    {
        var writer = new MarkoutWriter();
        writer.WriteDescriptions([
            new Description("Insight", "What does the generic math hierarchy look like?", "dotnet-inspect api System.Runtime"),
        ]);

        var expected = """
            - Insight: What does the generic math hierarchy look like?
              dotnet-inspect api System.Runtime
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteDescriptions_Markdown_BoldsLabel()
    {
        var writer = new MarkdownWriter();
        writer.WriteDescriptions([
            new Description("Insight", "What does the generic math hierarchy look like?"),
            new Description("Discovery", "What can JsonSerializer do?"),
        ]);

        var expected = """
            - **Insight:** What does the generic math hierarchy look like?
            - **Discovery:** What can JsonSerializer do?
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteDescriptions_Markdown_WithDetail_IndentsDetail()
    {
        var writer = new MarkdownWriter();
        writer.WriteDescriptions([
            new Description("Insight", "Hierarchy overview", "dotnet-inspect api System.Runtime"),
        ]);

        var expected = """
            - **Insight:** Hierarchy overview
              dotnet-inspect api System.Runtime
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteCallout_PlainText_WritesLabelAndMessage()
    {
        var writer = new MarkoutWriter();
        writer.WriteCallout(CalloutSeverity.Warning, "3 known vulnerabilities found.");

        var output = writer.ToString();
        Assert.Contains("WARNING: 3 known vulnerabilities found.", output);
    }

    [Fact]
    public void WriteCallout_Markdown_WritesGitHubFlavor()
    {
        var writer = new MarkdownWriter();
        writer.WriteCallout(CalloutSeverity.Warning, "3 known vulnerabilities found.");

        var expected = """
            > [!WARNING]
            > 3 known vulnerabilities found.
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteCallout_Markdown_AllSeverities()
    {
        foreach (var severity in Enum.GetValues<CalloutSeverity>())
        {
            var writer = new MarkdownWriter();
            writer.WriteCallout(severity, "test");
            var output = writer.ToString();
            Assert.Contains($"> [!{severity.ToString().ToUpperInvariant()}]", output);
        }
    }

    [Fact]
    public void WriteCallout_Markdown_Caution_RendersCorrectly()
    {
        var writer = new MarkdownWriter();
        writer.WriteCallout(CalloutSeverity.Caution, "This package has critical vulnerabilities.");

        var expected = """
            > [!CAUTION]
            > This package has critical vulnerabilities.
            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteCallout_Markdown_Note_RendersCorrectly()
    {
        var writer = new MarkdownWriter();
        writer.WriteCallout(CalloutSeverity.Note, "SourceLink is available.");

        var expected = """
            > [!NOTE]
            > SourceLink is available.
            """;
        Assert.Equal(expected, writer.ToString());
    }

    #region Distribution

    [Fact]
    public void WriteBreakdown_PlainText_RendersStackedBars()
    {
        var writer = new MarkoutWriter();
        var items = new List<Breakdown>
        {
            new("Jan 2025", [new("Critical", 1), new("High", 3)]),
            new("Feb 2025", [new("Critical", 2), new("High", 1)]),
        };
        writer.WriteBreakdown(items);

        var output = writer.ToString();
        Assert.Contains("Jan 2025", output);
        Assert.Contains("Feb 2025", output);
        Assert.Contains("█", output);
        Assert.Contains("▓", output);
        Assert.Contains("1 Critical, 3 High", output);
        Assert.Contains("2 Critical, 1 High", output);
        // Legend
        Assert.Contains("█ Critical", output);
        Assert.Contains("▓ High", output);
    }

    [Fact]
    public void WriteBreakdown_Markdown_RendersAsTable()
    {
        var writer = new MarkdownWriter();
        var items = new List<Breakdown>
        {
            new("v9.0", [new("Critical", 2), new("High", 4)]),
        };
        writer.WriteBreakdown(items);

        var output = writer.ToString();
        Assert.Contains("| Category | Count | % |", output);
        Assert.Contains("Critical", output);
        Assert.Contains("High", output);
        Assert.DoesNotContain("```text", output);
    }

    [Fact]
    public void WriteBreakdown_EmptyItems_WritesNothing()
    {
        var writer = new MarkoutWriter();
        writer.WriteBreakdown(new List<Breakdown>());
        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void WriteBreakdown_ScaledWidth_ProportionalBars()
    {
        var writer = new MarkoutWriter();
        var items = new List<Breakdown>
        {
            new("Row1", [new("A", 10), new("B", 20)]),
            new("Row2", [new("A", 5), new("B", 5)]),
        };
        writer.WriteBreakdown(items, maxBarWidth: 30);

        var output = writer.ToString();
        // The longest row (30 total) should use full width
        Assert.Contains("Row1", output);
        Assert.Contains("Row2", output);
    }

    #endregion

    #region Quotation Tests

    [Fact]
    public void WriteQuotation_PlainText_IndentsText()
    {
        var writer = new MarkoutWriter();
        writer.WriteQuotation("To be or not to be.");

        Assert.Contains("  To be or not to be.", writer.ToString());
    }

    [Fact]
    public void WriteQuotation_PlainText_MultiLine()
    {
        var writer = new MarkoutWriter();
        writer.WriteQuotation("Line one.\nLine two.");

        var output = writer.ToString();
        Assert.Contains("  Line one.", output);
        Assert.Contains("  Line two.", output);
    }

    [Fact]
    public void WriteQuotation_Markdown_UsesAngleBracket()
    {
        var writer = new MarkdownWriter();
        writer.WriteQuotation("A wise observation.");

        Assert.Contains("> A wise observation.", writer.ToString());
    }

    [Fact]
    public void WriteQuotation_Markdown_MultiLine()
    {
        var writer = new MarkdownWriter();
        writer.WriteQuotation("First line.\nSecond line.");

        var output = writer.ToString();
        Assert.Contains("> First line.", output);
        Assert.Contains("> Second line.", output);
    }

    #endregion

    #region Rule Tests

    [Fact]
    public void WriteRule_PlainText_WritesUnicodeLine()
    {
        var writer = new MarkoutWriter();
        writer.WriteRule();

        Assert.Contains("────────", writer.ToString());
    }

    [Fact]
    public void WriteRule_Markdown_WritesTripleDash()
    {
        var writer = new MarkdownWriter();
        writer.WriteRule();

        Assert.Contains("---", writer.ToString());
    }

    #endregion
}
