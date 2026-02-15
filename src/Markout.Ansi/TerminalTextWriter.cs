using System.Text;
using Microsoft.Extensions.Terminal;

namespace Markout.Ansi;

/// <summary>
/// TextWriter adapter that delegates to an ITerminal.
/// Bridges the ITerminal API to the TextWriter expected by MarkoutWriter's base class.
/// </summary>
internal sealed class TerminalTextWriter : TextWriter
{
    private readonly ITerminal _terminal;

    public TerminalTextWriter(ITerminal terminal) => _terminal = terminal;

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) => _terminal.Append(value);

    public override void Write(string? value)
    {
        if (value != null)
            _terminal.Append(value);
    }

    public override void WriteLine() => _terminal.AppendLine();

    public override void WriteLine(string? value) => _terminal.AppendLine(value ?? "");
}
