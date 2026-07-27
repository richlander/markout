using System.Text.RegularExpressions;
using Markout;
using Markout.Ansi.Spectre;
using Spectre.Console;

namespace Markout.Tests;

/// <summary>
/// <see cref="SpectreFormatter"/> lives in its own assembly and declares
/// <see cref="Markout.Formatting.IGlyphFormatter"/>, so it has to render
/// <see cref="TreeNodeState"/> like the other glyph sinks. It previously had no test coverage at
/// all, which is how it was missed when the state was introduced.
/// </summary>
public partial class SpectreTreeStateTests
{
    [Fact]
    public void RendersTheRevisitGlyph()
    {
        Assert.Contains("└─ \u21a9 B", Render(new MarkoutWriterOptions()));
    }

    [Fact]
    public void HonoursAConfiguredGlyph()
    {
        var options = new MarkoutWriterOptions { Glyphs = MarkoutGlyphs.Default with { Revisit = "[seen]" } };

        Assert.Contains("└─ [seen] B", Render(options));
    }

    [Fact]
    public void ANormalNodeGetsNoPrefix()
    {
        var output = Render(new MarkoutWriterOptions(), TreeNodeState.Normal);

        Assert.Contains("└─ B", output);
        Assert.DoesNotContain("\u21a9", output);
    }

    [Fact]
    public void TheStatePrecedesTheBadge()
    {
        Assert.Contains("└─ \u21a9 📁 B", Render(new MarkoutWriterOptions(), badge: "📁"));
    }

    /// <summary>
    /// The state is structure, not decoration, so dropping badges must not drop it.
    /// </summary>
    [Fact]
    public void IncludeBadgesDoesNotSuppressTheState()
    {
        var output = Render(new MarkoutWriterOptions { IncludeBadges = false }, badge: "📁");

        Assert.Contains("└─ \u21a9 B", output);
        Assert.DoesNotContain("📁", output);
    }

    private static string Render(
        MarkoutWriterOptions options,
        TreeNodeState state = TreeNodeState.Revisit,
        string? badge = null)
    {
        var writer = MarkoutWriter.Create(NewFormatter(), options);
        writer.WriteTree(new TreeNode("A", [new TreeNode("B") { State = state, Badge = badge }]));

        // Spectre wraps the connector in SGR escapes; strip them so assertions read as text.
        return AnsiEscape().Replace(writer.ToString(), "").Replace("\r\n", "\n");
    }

    private static SpectreFormatter NewFormatter()
        => new(AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(TextWriter.Null)
        }));

    [GeneratedRegex("\u001b\\[[0-9;]*m")]
    private static partial Regex AnsiEscape();
}
