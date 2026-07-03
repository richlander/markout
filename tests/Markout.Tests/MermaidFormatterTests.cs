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
        var result = MermaidFormatter.EscapeLabel("path\\to\\file");
        Assert.Equal("path\\\\to\\\\file", result);
    }

    [Fact]
    public void EscapeLabel_PlainTextPassesThrough()
    {
        var result = MermaidFormatter.EscapeLabel("System.Object");
        Assert.Equal("System.Object", result);
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
