using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes code blocks to a TextWriter using a code block formatter.
/// Tracks nesting state to prevent invalid overlapping code regions.
/// Document state is managed by the caller or <see cref="MarkoutWriter"/>.
/// </summary>
public class CodeBlockWriter(TextWriter writer, ICodeBlockFormatter formatter)
{
    private bool _inCode;

    /// <summary>
    /// Gets whether we are currently inside a code block.
    /// </summary>
    public bool InCode => _inCode;

    /// <summary>
    /// Starts a code region with optional language specifier.
    /// </summary>
    public void WriteCodeStart(string? language = null)
    {
        if (_inCode)
            throw new InvalidOperationException("Cannot nest code regions. End the current code region before starting a new one.");

        _inCode = true;
        formatter.FormatCodeStart(writer, language);
    }

    /// <summary>
    /// Writes a line of code inside a code block.
    /// </summary>
    public void WriteCodeLine(string text)
    {
        writer.WriteLine(text);
    }

    /// <summary>
    /// Ends a code region.
    /// </summary>
    public void WriteCodeEnd()
    {
        if (!_inCode)
            throw new InvalidOperationException("Cannot end a code region without starting one first.");

        _inCode = false;
        formatter.FormatCodeEnd(writer);
    }
}
