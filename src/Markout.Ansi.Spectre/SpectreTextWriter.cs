using System.Text;
using Spectre.Console;

namespace Markout.Ansi.Spectre;

/// <summary>
/// TextWriter adapter that delegates plain text writes to an IAnsiConsole.
/// Bridges the Spectre API to the TextWriter expected by MarkoutWriter's base class.
/// </summary>
internal sealed class SpectreTextWriter : TextWriter
{
    private readonly IAnsiConsole _console;

    public SpectreTextWriter(IAnsiConsole console) => _console = console;

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) => _console.Write(value.ToString());

    public override void Write(string? value)
    {
        if (value != null)
            _console.Write(new Text(value));
    }

    public override void WriteLine() => _console.WriteLine();

    public override void WriteLine(string? value)
    {
        _console.Write(new Text(value ?? ""));
        _console.WriteLine();
    }
}
