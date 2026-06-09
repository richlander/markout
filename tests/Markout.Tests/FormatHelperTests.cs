using Markout.Formatting;
using Xunit;

namespace Markout.Tests;

public class FormatHelperTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1_024, "1.0 KB")]
    [InlineData(1_536, "1.5 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(10_485_760, "10.0 MB")]
    [InlineData(1_073_741_824, "1.0 GB")]
    public void FormatSize_FormatsCorrectly(long bytes, string expected)
    {
        Assert.Equal(expected, FormatHelper.FormatSize(bytes));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1_000, "1.0K")]
    [InlineData(1_500, "1.5K")]
    [InlineData(1_000_000, "1.0M")]
    [InlineData(2_500_000, "2.5M")]
    [InlineData(1_000_000_000, "1.0B")]
    public void FormatDownloads_FormatsCorrectly(long count, string expected)
    {
        Assert.Equal(expected, FormatHelper.FormatDownloads(count));
    }

    [Fact]
    public void Truncate_ShortString_ReturnsUnchanged()
    {
        Assert.Equal("hello", FormatHelper.Truncate("hello", 10));
    }

    [Fact]
    public void Truncate_ExactLength_ReturnsUnchanged()
    {
        Assert.Equal("hello", FormatHelper.Truncate("hello", 5));
    }

    [Fact]
    public void Truncate_LongString_TruncatesWithEllipsis()
    {
        Assert.Equal("hell...", FormatHelper.Truncate("hello world", 7));
    }

    [Fact]
    public void Truncate_Null_ReturnsEmpty()
    {
        Assert.Equal("", FormatHelper.Truncate(null, 10));
    }

    [Fact]
    public void Truncate_CollapsesNewlines()
    {
        Assert.Equal("line1 line2", FormatHelper.Truncate("line1\nline2", 20));
        Assert.Equal("line1 line2", FormatHelper.Truncate("line1\r\nline2", 20));
    }

    [Fact]
    public void EscapeTableCell_NormalizesPipesToEntity()
    {
        Assert.Equal("left&#124;right", FormatHelper.EscapeTableCell("left|right"));
    }

    [Theory]
    [InlineData("ReturnType", "return_type")]
    [InlineData("Return Type", "return_type")]
    [InlineData("HTTP2Status", "http2_status")]
    [InlineData("Sim", "sim")]
    public void ToSnakeCase_FormatsStableNames(string value, string expected)
    {
        Assert.Equal(expected, FormatHelper.ToSnakeCase(value));
    }

    [Fact]
    public void RenderInlineMarkdown_RendersCodeTagsAsMarkdownCodeSpans()
    {
        Assert.Equal(
            "Use `List<T>` here",
            FormatHelper.RenderInlineMarkdown("Use <code>List&lt;T&gt;</code> here"));
    }

    [Fact]
    public void RenderInlineMarkdown_UsesLongerFenceWhenCodeContainsBackticks()
    {
        Assert.Equal(
            "Use `` `literal` `` here",
            FormatHelper.RenderInlineMarkdown("Use <code>`literal`</code> here"));
    }

    [Fact]
    public void RenderInlinePlainText_StripsCodeTagsAndDecodesXmlText()
    {
        Assert.Equal(
            "Use List<T> & Span<T> here",
            FormatHelper.RenderInlinePlainText("Use <code>List&lt;T&gt; &amp; Span&lt;T&gt;</code> here"));
    }

    [Fact]
    public void RenderInlinePlainText_LeavesUnmatchedCodeTagsUnchanged()
    {
        Assert.Equal(
            "Use <code>List&lt;T&gt; here",
            FormatHelper.RenderInlinePlainText("Use <code>List&lt;T&gt; here"));
    }
}
