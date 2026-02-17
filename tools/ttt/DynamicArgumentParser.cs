using MarkdownTable.Formatting;

namespace ttt;

/// <summary>
/// Two-pass CLI argument parser: core flags first, then mode parameters.
/// </summary>
public static class DynamicArgumentParser
{
    public static CommandLineOptions ParseArguments(string[] args)
    {
        // First pass: core CLI flags
        var (options, remainingArgs) = ParseCoreArguments(args);

        // Default mode
        var modeName = options.ModeName;
        if (string.IsNullOrEmpty(modeName))
            modeName = "smooth";

        // Create mode instance
        IFormatterMode? mode = null;
        if (!string.IsNullOrEmpty(modeName))
            FormatterRegistry.TryCreate(modeName, out mode);

        // Second pass: mode-specific parameters
        if (remainingArgs.Count > 0 && mode is not null)
        {
            ParseModeParameters(mode, modeName, remainingArgs);
        }
        else if (remainingArgs.Count > 0)
        {
            Console.Error.WriteLine($"Error: Unknown mode '{modeName}'");
            Environment.Exit(1);
        }

        return options with { ModeName = modeName, Mode = mode };
    }

    private static (CommandLineOptions Options, List<string> RemainingArgs) ParseCoreArguments(string[] args)
    {
        var options = new CommandLineOptions();
        var remainingArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    options = options with { ShowHelp = true };
                    break;

                case "--renderer" or "-r":
                    if (i + 1 < args.Length)
                        options = options with { ModeName = args[++i], ModeExplicit = true };
                    break;

                case "--show-modes":
                    options = options with { ShowModes = true };
                    break;

                default:
                    if (args[i].StartsWith("--"))
                    {
                        remainingArgs.Add(args[i]);
                        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                            remainingArgs.Add(args[++i]);
                    }
                    else if (!args[i].StartsWith('-'))
                    {
                        options = options with { InputFile = args[i] };
                    }
                    break;
            }
        }

        return (options, remainingArgs);
    }

    private static void ParseModeParameters(IFormatterMode mode, string modeName, List<string> remainingArgs)
    {
        var parameters = mode.GetParameters().ToDictionary(p => $"--{p.Name}", p => p);

        for (int i = 0; i < remainingArgs.Count; i += 2)
        {
            var flag = remainingArgs[i];
            if (i + 1 >= remainingArgs.Count)
            {
                Console.Error.WriteLine($"Error: Parameter '{flag}' requires a value");
                Environment.Exit(1);
                return;
            }

            var value = remainingArgs[i + 1];

            if (parameters.TryGetValue(flag, out var parameter))
            {
                if (!parameter.TrySetValue(value))
                {
                    Console.Error.WriteLine($"Error: Invalid value '{value}' for parameter '{flag}'");
                    Environment.Exit(1);
                    return;
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: Unknown parameter '{flag}' for mode '{modeName}'");
                if (parameters.Count > 0)
                    Console.Error.WriteLine($"Available parameters: {string.Join(", ", parameters.Keys)}");
                Environment.Exit(1);
                return;
            }
        }
    }
}
