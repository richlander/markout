using Markout;

namespace Markout.Tests;

public class UnicodeWriterTests
{
    [Fact]
    public void SupportedShapes_ReturnsAll()
    {
        var writer = new UnicodeWriter(TextWriter.Null);
        Assert.Equal(MarkoutShape.All, writer.SupportedShapes);
    }

    [Fact]
    public void WriteHeading_Level1_UsesRuleCharacters()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw) { ConsoleWidth = 40 };
        writer.WriteHeading(1, "Test Title");
        var output = sw.ToString();
        Assert.Contains("Test Title", output);
        Assert.Contains("─", output);
    }

    [Fact]
    public void WriteHeading_Level2_PlainText()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteHeading(2, "Subtitle");
        var output = sw.ToString();
        Assert.Contains("Subtitle", output);
        // Level 2 should not have rule characters on the same line
        var lines = output.Split('\n');
        var titleLine = lines.FirstOrDefault(l => l.Contains("Subtitle"));
        Assert.NotNull(titleLine);
        Assert.DoesNotContain("─", titleLine);
    }

    [Fact]
    public void WriteFieldBareList_KeyValueFormat()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteFieldBareList([new("Name", "Alice")]);
        var output = sw.ToString();
        Assert.Contains("Name: Alice", output);
    }

    [Fact]
    public void WriteTable_WithUnicodeRuleSeparator()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteTable(
            ["Name", "Age"],
            [["Alice", "30"], ["Bob", "25"]]);
        var output = sw.ToString();
        
        Assert.Contains("NAME", output);
        Assert.Contains("AGE", output);
        Assert.Contains("Alice", output);
        Assert.Contains("Bob", output);
        Assert.Contains("─", output); // Unicode rule separator
    }

    [Fact]
    public void WriteTable_WithMaxItems()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var writer = new UnicodeWriter(sw, options);
        writer.WriteTable(
            ["Name"],
            [["Alice"], ["Bob"], ["Carol"]]);
        var output = sw.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void StreamingTable_WithMaxItems()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var writer = new UnicodeWriter(sw, options);
        writer.WriteTableStart("Name");
        writer.WriteTableRow("Alice");
        writer.WriteTableRow("Bob");
        writer.WriteTableRow("Carol");
        writer.WriteTableEnd();
        var output = sw.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void WriteTree_WithUnicodeConnectors()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteTree(
            new TreeNode("Root", null,
                new TreeNode("Child A"),
                new TreeNode("Child B")));
        var output = sw.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Child A", output);
        Assert.Contains("Child B", output);
        Assert.Contains("├─", output);
        Assert.Contains("└─", output);
    }

    [Fact]
    public void WriteTree_NestedStructure()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteTree(
            new TreeNode("Root", null,
                new TreeNode("Parent", null,
                    new TreeNode("Child 1"),
                    new TreeNode("Child 2"))));
        var output = sw.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Parent", output);
        Assert.Contains("Child 1", output);
        Assert.Contains("Child 2", output);
        Assert.Contains("├─", output); // Branch connector
        Assert.Contains("└─", output); // Last branch connector
    }

    [Fact]
    public void WriteTree_VerticalLineInPrefix()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteTree(
            new TreeNode("Root", null,
                new TreeNode("Parent 1", null,
                    new TreeNode("Child")),
                new TreeNode("Parent 2")));
        var output = sw.ToString();
        Assert.Contains("│", output); // Vertical line should appear in prefix
    }

    [Fact]
    public void WriteTree_WithBadges()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { IncludeBadges = true };
        var writer = new UnicodeWriter(sw, options);
        writer.WriteTree([
            new TreeNode("Item") { Badge = "OK" }
        ]);
        var output = sw.ToString();
        Assert.Contains("Item", output);
        Assert.Contains("[OK]", output);
    }

    [Fact]
    public void WriteListItem_WithBullet()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteListItem("Item one");
        var output = sw.ToString();
        Assert.Contains("• Item one", output);
    }

    [Fact]
    public void WriteArray_WithBullets()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteArray("Colors", ["Red", "Green", "Blue"]);
        var output = sw.ToString();
        Assert.Contains("Colors:", output);
        Assert.Contains("• Red", output);
        Assert.Contains("• Green", output);
        Assert.Contains("• Blue", output);
    }

    [Fact]
    public void WriteFieldLine_WithVerticalBar()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteFieldLine([
            new MarkoutField("Name", "Alice"),
            new MarkoutField("Age", "30")
        ]);
        var output = sw.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Alice", output);
        Assert.Contains("│", output); // Vertical bar separator
    }

    [Fact]
    public void WriteDescriptions_WithBulletAndDash()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteDescriptions([
            new Description("Term", "Definition"),
            new Description("Label", "Description")
        ]);
        var output = sw.ToString();
        Assert.Contains("• Term — Definition", output);
        Assert.Contains("• Label — Description", output);
    }

    [Fact]
    public void WriteCallout_Note()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteCallout(CalloutSeverity.Note, "Information");
        var output = sw.ToString();
        Assert.Contains("ℹ", output);
        Assert.Contains("Information", output);
    }

    [Fact]
    public void WriteCallout_Warning()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteCallout(CalloutSeverity.Warning, "Warning message");
        var output = sw.ToString();
        Assert.Contains("⚠", output);
        Assert.Contains("Warning message", output);
    }

    [Fact]
    public void WriteCallout_Caution()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteCallout(CalloutSeverity.Caution, "Danger!");
        var output = sw.ToString();
        Assert.Contains("✖", output);
        Assert.Contains("Danger!", output);
    }

    [Fact]
    public void WriteCallout_Tip()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteCallout(CalloutSeverity.Tip, "Helpful hint");
        var output = sw.ToString();
        Assert.Contains("💡", output);
        Assert.Contains("Helpful hint", output);
    }

    [Fact]
    public void WriteQuotation_WithVerticalBar()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteQuotation("This is a quote");
        var output = sw.ToString();
        Assert.Contains("│ This is a quote", output);
    }

    [Fact]
    public void WriteQuotation_Multiline()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteQuotation("Line 1\nLine 2");
        var output = sw.ToString();
        Assert.Contains("│ Line 1", output);
        Assert.Contains("│ Line 2", output);
    }

    [Fact]
    public void WriteRule_UsesRuleCharacter()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw) { ConsoleWidth = 40 };
        writer.WriteRule();
        var output = sw.ToString();
        Assert.Contains("─────", output); // Multiple rule characters
    }

    [Fact]
    public void WriteMetrics_WithBarCharacter()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteMetrics([
            new Metric("CPU", 80),
            new Metric("Memory", 60),
            new Metric("Disk", 40)
        ], maxBarWidth: 20);
        var output = sw.ToString();
        Assert.Contains("CPU", output);
        Assert.Contains("Memory", output);
        Assert.Contains("Disk", output);
        Assert.Contains("█", output); // Bar character
    }

    [Fact]
    public void WriteBreakdown_WithBars()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteBreakdown([
            new Breakdown("Windows", [
                new Segment("Critical", 10),
                new Segment("High", 20),
                new Segment("Medium", 30)
            ]),
            new Breakdown("Linux", [
                new Segment("Critical", 5),
                new Segment("High", 15),
                new Segment("Medium", 20)
            ])
        ], maxBarWidth: 30);
        var output = sw.ToString();
        Assert.Contains("Windows", output);
        Assert.Contains("Linux", output);
        Assert.Contains("█", output);
    }

    [Fact]
    public void WriteVerticalMetrics_WithBarCharacter()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteVerticalMetrics([
            new Metric("Jan", 100),
            new Metric("Feb", 80),
            new Metric("Mar", 90)
        ], maxBarHeight: 5, barWidth: 6);
        var output = sw.ToString();
        Assert.Contains("Jan", output);
        Assert.Contains("Feb", output);
        Assert.Contains("Mar", output);
        Assert.Contains("█", output);
    }

    [Fact]
    public void ConsoleWidth_DefaultIs80()
    {
        var writer = new UnicodeWriter(TextWriter.Null);
        Assert.Equal(80, writer.ConsoleWidth);
    }

    [Fact]
    public void ConsoleWidth_CanBeSet()
    {
        var writer = new UnicodeWriter(TextWriter.Null) { ConsoleWidth = 120 };
        Assert.Equal(120, writer.ConsoleWidth);
    }

    [Fact]
    public void ConsoleWidth_RejectsInvalidValues()
    {
        var writer = new UnicodeWriter(TextWriter.Null) { ConsoleWidth = -10 };
        Assert.Equal(80, writer.ConsoleWidth); // Should fall back to 80
    }

    [Fact]
    public void RuleCharacter_CanBeCustomized()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw) { RuleCharacter = '═', ConsoleWidth = 20 };
        writer.WriteHeading(1, "Test");
        var output = sw.ToString();
        Assert.Contains("═", output);
        Assert.DoesNotContain("─", output);
    }

    [Fact]
    public void BarCharacter_CanBeCustomized()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw) { BarCharacter = '▬' };
        writer.WriteMetrics([
            new Metric("Test", 50)
        ], maxBarWidth: 10);
        var output = sw.ToString();
        Assert.Contains("▬", output);
        Assert.DoesNotContain("█", output);
    }

    [Fact]
    public void WriteCodeStart_DoesNotThrow()
    {
        var sw = new StringWriter();
        var writer = new UnicodeWriter(sw);
        writer.WriteCodeStart("csharp");
        writer.WriteCodeEnd();
        // Just verify it doesn't throw
    }

    [Fact]
    public void WriteCodeStart_Nested_Throws()
    {
        var writer = new UnicodeWriter(TextWriter.Null);
        writer.WriteCodeStart();
        Assert.Throws<InvalidOperationException>(() => writer.WriteCodeStart());
    }

    [Fact]
    public void WriteCodeEnd_WithoutStart_Throws()
    {
        var writer = new UnicodeWriter(TextWriter.Null);
        Assert.Throws<InvalidOperationException>(() => writer.WriteCodeEnd());
    }
}
