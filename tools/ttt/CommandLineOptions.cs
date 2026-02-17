using MarkdownTable.Formatting;

namespace ttt;

public record CommandLineOptions(
    string? InputFile = null,
    bool ShowHelp = false,
    bool ShowModes = false,
    string ModeName = "",
    bool ModeExplicit = false,
    IFormatterMode? Mode = null
);
