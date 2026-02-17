using MarkdownTable.Formatting;

namespace Markout.Templates;

/// <summary>
/// Parses template text into a sequence of <see cref="TemplateNode"/> elements.
/// Recognizes ATX headings, block-level placeholders, conditional sections,
/// blank lines, and prose paragraphs with optional inline placeholders.
/// </summary>
public static class TemplateParser
{
    private const string OpenSymbol = "{{";
    private const string CloseSymbol = "}}";

    /// <summary>
    /// Parses template text into an ordered list of nodes.
    /// </summary>
    public static List<TemplateNode> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var nodes = new List<TemplateNode>();
        List<string>? paragraphLines = null;
        List<string>? tableLines = null;

        using var reader = new StringReader(text);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            // Blank line
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushTable(nodes, ref tableLines);
                FlushParagraph(nodes, ref paragraphLines);
                nodes.Add(new BlankLineNode());
                continue;
            }

            // If we're accumulating table lines, check if this continues the table
            if (tableLines is not null)
            {
                if (TableParser.IsPipeTableLine(line))
                {
                    tableLines.Add(line);
                    continue;
                }

                // Not a table line — flush the table and fall through
                FlushTable(nodes, ref tableLines);
            }

            // Conditional: {{#if key}} or {{/if}}
            if (TryParseConditional(line, out var conditionalNode))
            {
                FlushParagraph(nodes, ref paragraphLines);
                nodes.Add(conditionalNode);
                continue;
            }

            // Block-level placeholder: {{key}} as the entire line
            if (TryParseBlockPlaceholder(line, out var key))
            {
                FlushParagraph(nodes, ref paragraphLines);
                nodes.Add(new PlaceholderNode(key));
                continue;
            }

            // ATX heading: # through ######
            if (TryParseHeading(line, out int level, out string? headingText))
            {
                FlushParagraph(nodes, ref paragraphLines);
                nodes.Add(new HeadingNode(level, headingText));
                continue;
            }

            // Pipe table start — a line with pipes that isn't a heading or placeholder
            if (TableParser.IsPipeTableLine(line))
            {
                FlushParagraph(nodes, ref paragraphLines);
                tableLines = [line];
                continue;
            }

            // Prose — accumulate into paragraph
            paragraphLines ??= [];
            paragraphLines.Add(line);
        }

        FlushTable(nodes, ref tableLines);
        FlushParagraph(nodes, ref paragraphLines);
        return nodes;
    }

    private static void FlushParagraph(List<TemplateNode> nodes, ref List<string>? lines)
    {
        if (lines is null || lines.Count == 0)
            return;

        var text = string.Join('\n', lines);
        nodes.Add(new ParagraphNode(text));
        lines = null;
    }

    private static void FlushTable(List<TemplateNode> nodes, ref List<string>? lines)
    {
        if (lines is null || lines.Count == 0)
            return;

        if (TableParser.TryParse(lines, out var headers, out var rows))
        {
            nodes.Add(new TableNode(headers, rows));
        }
        else
        {
            // Not a valid table — treat as paragraph text
            var text = string.Join('\n', lines);
            nodes.Add(new ParagraphNode(text));
        }

        lines = null;
    }

    private static bool TryParseBlockPlaceholder(string line, out string key)
    {
        var trimmed = line.AsSpan().Trim();

        if (trimmed.StartsWith(OpenSymbol) && trimmed.EndsWith(CloseSymbol))
        {
            var inner = trimmed[2..^2].Trim();

            // Must be a simple key (no spaces, no directives like #if)
            if (inner.Length > 0 && !inner.Contains(' ') && inner[0] != '#' && inner[0] != '/')
            {
                key = inner.ToString();
                return true;
            }
        }

        key = "";
        return false;
    }

    private static bool TryParseConditional(string line, out TemplateNode node)
    {
        var trimmed = line.AsSpan().Trim();

        if (trimmed.StartsWith(OpenSymbol) && trimmed.EndsWith(CloseSymbol))
        {
            var inner = trimmed[2..^2].Trim();

            if (inner.StartsWith("#if "))
            {
                var key = inner[4..].Trim();
                if (key.Length > 0)
                {
                    node = new ConditionalStartNode(key.ToString());
                    return true;
                }
            }
            else if (inner is "/if")
            {
                node = new ConditionalEndNode();
                return true;
            }
        }

        node = default!;
        return false;
    }

    private static bool TryParseHeading(string line, out int level, out string text)
    {
        var span = line.AsSpan();
        level = 0;

        while (level < span.Length && level < 6 && span[level] == '#')
            level++;

        if (level > 0 && level < span.Length && span[level] == ' ')
        {
            text = span[(level + 1)..].ToString();
            return true;
        }

        level = 0;
        text = "";
        return false;
    }

    /// <summary>
    /// Resolves inline {{key}} placeholders in text using the provided lookup function.
    /// </summary>
    /// <summary>
    /// Resolves inline {{key}} placeholders in text using the provided lookup function.
    /// Returns the text with all known keys replaced and unknown keys preserved.
    /// </summary>
    public static string ResolveInlinePlaceholders(string text, Func<string, string?> lookup)
    {
        if (!text.Contains(OpenSymbol))
            return text;

        var span = text.AsSpan();
        var result = new System.Text.StringBuilder(text.Length);
        int pos = 0;

        while (pos < span.Length)
        {
            int openIndex = span[pos..].IndexOf(OpenSymbol);

            if (openIndex < 0)
            {
                result.Append(span[pos..]);
                break;
            }

            // Append text before the placeholder
            result.Append(span[pos..(pos + openIndex)]);

            int keyStart = pos + openIndex + OpenSymbol.Length;
            int closeIndex = span[keyStart..].IndexOf(CloseSymbol);

            if (closeIndex < 0)
            {
                // No closing — append remainder as-is
                result.Append(span[(pos + openIndex)..]);
                break;
            }

            var key = span[keyStart..(keyStart + closeIndex)].Trim();
            var replacement = lookup(key.ToString());
            result.Append(replacement ?? $"{OpenSymbol}{key}{CloseSymbol}");

            pos = keyStart + closeIndex + CloseSymbol.Length;
        }

        return result.ToString();
    }
}
