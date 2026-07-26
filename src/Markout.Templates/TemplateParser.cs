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
        ValidateConditionalBalance(nodes);
        return nodes;
    }

    /// <summary>
    /// Verifies that block-level <c>{{#if}}</c> / <c>{{/if}}</c> directives are balanced. An
    /// unclosed <c>{{#if}}</c> would otherwise silently drop the remainder of the document at render
    /// time (data-dependent on the key's truthiness), and a stray <c>{{/if}}</c> indicates an
    /// authoring error; both are surfaced eagerly as a <see cref="FormatException"/>.
    /// </summary>
    private static void ValidateConditionalBalance(List<TemplateNode> nodes)
    {
        int depth = 0;
        foreach (var node in nodes)
        {
            switch (node)
            {
                case ConditionalStartNode:
                    depth++;
                    break;
                case ConditionalEndNode:
                    if (depth == 0)
                        throw new FormatException(
                            "Unbalanced template conditional: '{{/if}}' without a matching '{{#if}}'.");
                    depth--;
                    break;
            }
        }
        if (depth > 0)
            throw new FormatException(
                "Unbalanced template conditional: an '{{#if}}' block is missing its '{{/if}}'.");
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

            // Must be a simple key (no spaces, no nested braces, no directives like #if).
            // Nested braces mean this line carries inline content (e.g. "{{#if x}}{{y}}") and
            // must fall through to a paragraph for inline resolution rather than being treated
            // as a single block placeholder.
            if (inner.Length > 0
                && !inner.Contains(' ')
                && inner.IndexOf(OpenSymbol) < 0
                && inner.IndexOf(CloseSymbol) < 0
                && inner[0] != '#' && inner[0] != '/')
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

            // A standalone block directive must be the only thing on the line. If it carries nested
            // braces it is an inline conditional wrapping content (e.g. "{{#if x}}text{{/if}}") and
            // is handled during inline resolution inside a paragraph, not as a block node.
            if (inner.IndexOf(OpenSymbol) >= 0 || inner.IndexOf(CloseSymbol) >= 0)
            {
                node = default!;
                return false;
            }

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
    /// Resolves inline <c>{{#if key}}…{{/if}}</c> conditionals within a single text run (a heading
    /// or paragraph). Content inside a conditional is kept when <paramref name="isTruthy"/> returns
    /// true for its key, and dropped otherwise. Conditionals may nest. Non-conditional
    /// <c>{{key}}</c> placeholders are preserved verbatim for a later
    /// <see cref="ResolveInlinePlaceholders"/> pass.
    /// </summary>
    /// <remarks>
    /// Inline conditionals must be balanced within the same text run. Author a self-contained
    /// optional line as <c>{{#if key}}content{{/if}}</c> on one line so the surrounding lines stay
    /// tight; use standalone <c>{{#if key}}</c> / <c>{{/if}}</c> directive lines for multi-line
    /// block sections instead.
    /// </remarks>
    public static string ResolveInlineConditionals(string text, Func<string, bool> isTruthy)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(isTruthy);

        if (text.IndexOf("{{#if ", StringComparison.Ordinal) < 0
            && text.IndexOf("{{/if}}", StringComparison.Ordinal) < 0)
            return text;

        var result = new System.Text.StringBuilder(text.Length);
        int pos = 0;
        int skipDepth = 0;
        int openDepth = 0;

        while (pos < text.Length)
        {
            int open = text.IndexOf(OpenSymbol, pos, StringComparison.Ordinal);
            if (open < 0)
            {
                if (skipDepth == 0)
                    result.Append(text, pos, text.Length - pos);
                break;
            }

            if (skipDepth == 0)
                result.Append(text, pos, open - pos);

            int close = text.IndexOf(CloseSymbol, open + OpenSymbol.Length, StringComparison.Ordinal);
            if (close < 0)
            {
                // Unterminated braces — treat the remainder as literal.
                if (skipDepth == 0)
                    result.Append(text, open, text.Length - open);
                break;
            }

            var inner = text.AsSpan((open + OpenSymbol.Length)..close).Trim();

            if (inner.StartsWith("#if "))
            {
                var key = inner[4..].Trim().ToString();
                if (key.Length == 0)
                    throw new FormatException(
                        "Inline conditional '{{#if}}' requires a key.");
                openDepth++;
                if (skipDepth > 0)
                {
                    skipDepth++;
                }
                else
                {
                    if (!isTruthy(key))
                        skipDepth = 1;
                }
            }
            else if (inner.SequenceEqual("#if"))
            {
                throw new FormatException(
                    "Inline conditional '{{#if}}' requires a key.");
            }
            else if (inner.SequenceEqual("/if"))
            {
                if (openDepth == 0)
                    throw new FormatException(
                        "Unbalanced inline conditional: '{{/if}}' without a matching '{{#if}}'.");
                openDepth--;
                if (skipDepth > 0)
                    skipDepth--;
            }
            else if (skipDepth == 0)
            {
                // A normal placeholder or other braces: keep verbatim for the placeholder pass.
                result.Append(text, open, close + CloseSymbol.Length - open);
            }

            pos = close + CloseSymbol.Length;
        }

        if (openDepth > 0)
            throw new FormatException(
                "Unbalanced inline conditional: an '{{#if}}' is missing its '{{/if}}'.");

        return result.ToString();
    }

    /// <summary>
    /// Resolves inline {{key}} placeholders in text using the provided lookup function.
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
