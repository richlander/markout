using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class MermaidFormatterTests
{
    [Fact]
    public void MermaidFormatter_ImplementsExpectedInterfaces()
    {
        var formatter = new MermaidFormatter();
        Assert.IsAssignableFrom<IMarkoutFormatter>(formatter);
        Assert.IsAssignableFrom<IHeadingFormatter>(formatter);
        Assert.IsAssignableFrom<ITreeFormatter>(formatter);
    }

    [Fact]
    public void MermaidFormatter_DoesNotImplementUnsupportedInterfaces()
    {
        var formatter = new MermaidFormatter();
        Assert.IsNotAssignableFrom<ITableFormatter>(formatter);
        Assert.IsNotAssignableFrom<IFieldFormatter>(formatter);
        Assert.IsNotAssignableFrom<IListFormatter>(formatter);
        Assert.IsNotAssignableFrom<ICodeBlockFormatter>(formatter);
        Assert.IsNotAssignableFrom<IBlockFormatter>(formatter);
        Assert.IsNotAssignableFrom<IMetricsFormatter>(formatter);
    }

    [Fact]
    public void WriteHeading_RendersAsMermaidComment()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteHeading(1, "My Diagram");
        Assert.Contains("%% My Diagram", orch.ToString());
    }

    [Fact]
    public void WriteHeading_WithContext_RendersContextInParens()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteHeading(1, "Dependencies", "v1.0");
        var output = orch.ToString();
        Assert.Contains("%% Dependencies (v1.0)", output);
    }

    [Fact]
    public void WriteTree_RendersGraphTD()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("Root", [
                new TreeNode("Child A"),
                new TreeNode("Child B")]));
        var output = orch.ToString();
        Assert.Contains("graph TD", output);
    }

    [Fact]
    public void WriteTree_RendersNodeLabels()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("Root", [
                new TreeNode("Child A"),
                new TreeNode("Child B")]));
        var output = orch.ToString();
        Assert.Contains("\"Root\"", output);
        Assert.Contains("\"Child A\"", output);
        Assert.Contains("\"Child B\"", output);
    }

    [Fact]
    public void WriteTree_RendersEdges()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("Root", [
                new TreeNode("Child A"),
                new TreeNode("Child B")]));
        var output = orch.ToString();
        // Root is n0, children are n1 and n2
        Assert.Contains("n0 --> n1", output);
        Assert.Contains("n0 --> n2", output);
    }

    [Fact]
    public void WriteTree_DeepHierarchy_RendersAllLevels()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("A", [
                new TreeNode("B", [
                    new TreeNode("C")])]));
        var output = orch.ToString();
        Assert.Contains("\"A\"", output);
        Assert.Contains("\"B\"", output);
        Assert.Contains("\"C\"", output);
        // A->B, B->C
        Assert.Contains("n0 --> n1", output);
        Assert.Contains("n1 --> n2", output);
    }

    [Fact]
    public void WriteTree_MultipleRoots_RendersAll()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("Root1"),
            new TreeNode("Root2"));
        var output = orch.ToString();
        Assert.Contains("\"Root1\"", output);
        Assert.Contains("\"Root2\"", output);
        // Root nodes have no parent edges
        Assert.DoesNotContain("-->", output);
    }

    [Fact]
    public void WriteTree_WithBadges_IncludesBadgeInLabel()
    {
        var options = new MarkoutWriterOptions { IncludeBadges = true };
        var orch = MarkoutWriter.Create(new MermaidFormatter(), options);
        orch.WriteTree(new TreeNode("Libraries") { Badge = "📁" });
        var output = orch.ToString();
        Assert.Contains("📁 Libraries", output);
    }

    [Fact]
    public void WriteTree_BadgesDisabled_ExcludesBadge()
    {
        var options = new MarkoutWriterOptions { IncludeBadges = false };
        var orch = MarkoutWriter.Create(new MermaidFormatter(), options);
        orch.WriteTree(new TreeNode("Libraries") { Badge = "📁" });
        var output = orch.ToString();
        Assert.DoesNotContain("📁", output);
        Assert.Contains("\"Libraries\"", output);
    }

    [Fact]
    public void WriteTable_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        var result = orch.WriteTable(["Col1"], [["val1"]]);
        Assert.False(result);
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void WriteFields_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        var result = orch.WriteFields(new MarkoutField("Key", "Value"));
        Assert.False(result);
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void WriteList_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        var result = orch.WriteList("one", "two");
        Assert.False(result);
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void EscapeLabel_EscapesDoubleQuotes()
    {
        var result = MermaidFormatter.EscapeLabel("hello \"world\"");
        Assert.Equal("hello #quot;world#quot;", result);
    }

    [Fact]
    public void EscapeLabel_EscapesBackslashes()
    {
        // Mermaid performs no backslash unescaping in quoted labels, so a doubled
        // backslash would render as two backslashes. It also rewrites the two-character
        // sequence \n into a line break while splitting rows, which would wrap
        // "C:\new" mid-word. The entity form avoids both.
        var result = MermaidFormatter.EscapeLabel("path\\to\\file");
        Assert.Equal("path#92;to#92;file", result);
    }

    [Fact]
    public void EscapeLabel_PlainTextPassesThrough()
    {
        var result = MermaidFormatter.EscapeLabel("System.Object");
        Assert.Equal("System.Object", result);
    }

    [Fact]
    public void EscapeLabel_EscapesAngleBrackets()
    {
        // Flowcharts default to HTML labels and DOMPurify drops unknown tags at the
        // default strict security level, so an unescaped generic name would render as
        // "System.IComparable" and collide with the non-generic type.
        var result = MermaidFormatter.EscapeLabel("System.IComparable<TSelf>");
        Assert.Equal("System.IComparable#60;TSelf#62;", result);
    }

    [Theory]
    // Generic and compiler-generated .NET names: the motivating corpus.
    [InlineData("List<String>", "List#60;String#62;")]
    [InlineData("<Main>$", "#60;Main#62;$")]
    [InlineData("<>c__DisplayClass0_0", "#60;#62;c__DisplayClass0_0")]
    [InlineData("<Foo>b__0", "#60;Foo#62;b__0")]
    [InlineData("IDictionary<K, V>.TryGetValue(K, V&)", "IDictionary#60;K, V#62;.TryGetValue(K, V#38;)")]
    // Structural characters that would otherwise alter the diagram.
    [InlineData("a|b", "a#124;b")]
    [InlineData("a&b", "a#38;b")]
    [InlineData("line1\nline2", "line1#10;line2")]
    [InlineData("line1\r\nline2", "line1#13;#10;line2")]
    public void EscapeLabel_HostileNames_AreEscaped(string input, string expected)
        => Assert.Equal(expected, MermaidFormatter.EscapeLabel(input));

    [Fact]
    public void EscapeLabel_EscapesHashFirstSoEntitiesAreNotDoubleEscaped()
    {
        // '#' introduces the entity form, so a literal '#' must itself be escaped;
        // otherwise text that already looks like an entity would be misread on render.
        Assert.Equal("C#35;", MermaidFormatter.EscapeLabel("C#"));
        Assert.Equal("#35;quot;", MermaidFormatter.EscapeLabel("#quot;"));
    }

    [Theory]
    [InlineData("`boom")]
    [InlineData("`pwned`")]
    [InlineData("List`1")]
    public void EscapeLabel_EscapesBacktick(string input)
    {
        // A label is emitted as ["…], so a leading backtick forms the sequence ["` that
        // Mermaid lexes as the start of a Markdown string — a rule that both precedes and
        // outranks the plain [" rule. An unterminated one is a fatal lexical error; a
        // terminated one silently reparses the node as Markdown.
        var escaped = MermaidFormatter.EscapeLabel(input);
        Assert.DoesNotContain("`", escaped);
        Assert.Contains("#96;", escaped);
    }

    [Fact]
    public void EscapeLabel_EscapesColonSoUpstreamStyleGuardsCannotMatch()
    {
        // Before decoding entities Mermaid runs two unanchored guards meant for real
        // style/classDef lines — /style.*:\S*#.*;/ and /classDef.*:\S*#.*;/ — each
        // stripping the last character it matched. Without escaping the colon,
        // "Lifestyle:C#" would escape to "Lifestyle:C#35;" and then be truncated to
        // "Lifestyle:C#35", destroying the entity. Neither guard can match without a colon.
        Assert.Equal("Lifestyle#58;C#35;", MermaidFormatter.EscapeLabel("Lifestyle:C#"));
        Assert.Equal("classDef x#58;#60;", MermaidFormatter.EscapeLabel("classDef x:<"));
        Assert.DoesNotContain(":", MermaidFormatter.EscapeLabel("style:#quot;>evil<"));
    }

    [Fact]
    public void EscapeLabel_QuoteCannotBreakOutOfLabel()
    {
        // The lexer ends a quoted label at the first '"' with no escape mechanism, so a
        // surviving quote would terminate the label and let the rest inject graph syntax.
        var escaped = MermaidFormatter.EscapeLabel("evil\"] --> boom[\"pwned");
        Assert.DoesNotContain("\"", escaped);
    }

    [Fact]
    public void WriteTree_GenericNodeLabels_AreEscapedInOutput()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("System.IComparable", [
                new TreeNode("System.IComparable<TSelf>")]));
        var output = orch.ToString();
        // The two nodes must remain distinguishable after rendering.
        Assert.Contains("n0[\"System.IComparable\"]", output);
        Assert.Contains("n1[\"System.IComparable#60;TSelf#62;\"]", output);
    }

    [Fact]
    public void WriteHeading_WithNewline_StaysOnOneCommentLine()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteHeading(1, "Title\ngraph TD");
        var output = orch.ToString().TrimEnd();
        // A Mermaid comment ends at the newline, so an unsanitized heading could emit
        // raw diagram syntax on the following line.
        Assert.Contains("%% Title graph TD", output);
        Assert.DoesNotContain("\n", output);
    }

    [Fact]
    public void WriteTree_UniqueNodeIds_AcrossSubtrees()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTree(
            new TreeNode("A", [
                new TreeNode("A1"),
                new TreeNode("A2")]),
            new TreeNode("B", [
                new TreeNode("B1")]));
        var output = orch.ToString();
        // 5 nodes total: n0 (A), n1 (A1), n2 (A2), n3 (B), n4 (B1)
        Assert.Contains("n0[", output);
        Assert.Contains("n1[", output);
        Assert.Contains("n2[", output);
        Assert.Contains("n3[", output);
        Assert.Contains("n4[", output);
    }

    [Fact]
    public void WriteTreeNode_RendersSingleNodeGraph()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteTreeNode("Standalone Node");
        var output = orch.ToString();
        Assert.Contains("graph TD", output);
        Assert.Contains("\"Standalone Node\"", output);
    }
}
