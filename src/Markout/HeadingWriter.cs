using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes headings to a TextWriter using a heading formatter.
/// Handles level validation. Document state (blank lines, sections) is
/// managed by the caller or <see cref="MarkoutWriter"/>.
/// </summary>
public class HeadingWriter(TextWriter writer, IHeadingFormatter formatter)
{
    /// <summary>
    /// Writes a heading at the specified level.
    /// </summary>
    public void WriteHeading(int level, string text, string? context = null)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        formatter.FormatHeading(writer, level, text, context);
        writer.WriteLine();
    }
}
