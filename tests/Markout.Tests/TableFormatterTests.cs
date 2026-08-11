using Markout;
using Markout.Formatting;
using System.Text.Json;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class TableFormatterTests
{
    [Fact]
    public void SupportedShapes_ReturnsTablesListsAndFields()
    {
        var formatter = new TableFormatter();
        Assert.IsAssignableFrom<ITableFormatter>(formatter);
        Assert.IsAssignableFrom<IListFormatter>(formatter);
        Assert.IsAssignableFrom<IFieldFormatter>(formatter);
    }

    [Fact]
    public void WriteTable_SpacePaddedColumns()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteTable(
            ["Name", "Age"],
            [["Alice", "30"], ["Bob", "7"]]);
        var output = sw.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Age", output);
        Assert.Contains("Alice", output);
        // Space-padded, not tab-separated
        Assert.DoesNotContain("\t", output);
    }

    [Fact]
    public void WriteTable_NoHeader()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(showHeader: false));
        writer.WriteTable(
            ["Name", "Age"],
            [["Alice", "30"]]);
        var output = sw.ToString();
        Assert.DoesNotContain("Name", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void TableFormatter_TsvMode_UsesStableHeadersByDefault()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Return Type", "Sim"],
            ["ReturnType", "Similarity"],
            [["string", "0.50"]]);

        Assert.Equal(
            "return_type\tsimilarity\nstring\t0.50\n",
            sw.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TableFormatter_TsvMode_CanUseDisplayHeaders()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            TableMode = MarkoutTableMode.Tsv,
            TableHeaderStyle = MarkoutTableHeaderStyle.DisplayName
        };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Return Type", "Sim"],
            ["ReturnType", "Similarity"],
            [["string", "0.50"]]);

        Assert.Equal(
            "Return Type\tSim\nstring\t0.50\n",
            sw.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TableFormatter_JsonlMode_UsesStableHeadersByDefault()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Return Type", "Sim"],
            ["ReturnType", "Similarity"],
            [["string", "0.50"]]);

        Assert.Equal(
            "{\"return_type\":\"string\",\"similarity\":\"0.50\"}\n",
            sw.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TableFormatter_JsonlMode_CanUseDisplayHeaders()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            TableMode = MarkoutTableMode.Jsonl,
            TableHeaderStyle = MarkoutTableHeaderStyle.DisplayName
        };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Return Type", "Sim"],
            ["ReturnType", "Similarity"],
            [["string", "0.50"]]);

        using var document = JsonDocument.Parse(sw.ToString());
        var root = document.RootElement;
        Assert.Equal("string", root.GetProperty("Return Type").GetString());
        Assert.Equal("0.50", root.GetProperty("Sim").GetString());
    }

    [Theory]
    [InlineData(MarkoutTableMode.Tsv)]
    [InlineData(MarkoutTableMode.Jsonl)]
    public void StructuredModes_IgnoreVisualHeaderFormatter(MarkoutTableMode mode)
    {
        var options = new MarkoutWriterOptions
        {
            TableMode = mode,
            FormatTableHeader = header => $"**{header.DisplayName}**"
        };
        var writer = MarkoutWriter.Create(new TableFormatter(), options);

        writer.WriteTable(["My Column"], ["MyColumn"], [["1"]]);

        Assert.Contains("my_column", writer.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("**", writer.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(MarkoutTableMode.Tsv)]
    [InlineData(MarkoutTableMode.Jsonl)]
    public void StructuredDisplayNameStyle_IgnoresFormatterButKeepsDisplayName(MarkoutTableMode mode)
    {
        var options = new MarkoutWriterOptions
        {
            TableMode = mode,
            TableHeaderStyle = MarkoutTableHeaderStyle.DisplayName,
            FormatTableHeader = header => $"**{header.DisplayName}**"
        };
        var writer = MarkoutWriter.Create(new TableFormatter(), options);

        writer.WriteTable(["My Column"], ["MyColumn"], [["1"]]);

        Assert.Contains("My Column", writer.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("**", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TableFormatter_JsonlMode_EscapesJsonSyntaxAndPreservesCellText()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Name", "Notes"],
            [["Alice \"A.\"", "line 1\nline 2\tTabbed"]]);

        var output = sw.ToString().ReplaceLineEndings("\n");
        Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("\\n", output);
        Assert.Contains("\\t", output);

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        Assert.Equal("Alice \"A.\"", root.GetProperty("name").GetString());
        Assert.Equal("line 1\nline 2\tTabbed", root.GetProperty("notes").GetString());
    }

    [Fact]
    public void TableFormatter_JsonlMode_DoesNotEmitTruncationFooter()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            TableMode = MarkoutTableMode.Jsonl,
            MaxItems = 1
        };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Name"],
            [["Alice"], ["Bob"], ["Carol"]]);

        var output = sw.ToString().ReplaceLineEndings("\n");
        Assert.Equal("{\"name\":\"Alice\"}\n", output);
        Assert.DoesNotContain("more", output);
    }

    [Fact]
    public void TableFormatter_PrettyMode_UsesDisplayHeadersByDefault()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteTable(
            ["Return Type", "Sim"],
            ["ReturnType", "Similarity"],
            [["string", "0.50"]]);

        var output = sw.ToString();
        Assert.Contains("Return Type", output);
        Assert.Contains("Sim", output);
        Assert.DoesNotContain('\t', output);
    }

    [Fact]
    public void WriteTable_WithMaxItems()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Name"],
            [["Alice"], ["Bob"], ["Carol"]]);
        var output = sw.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void StreamingTable_WithMaxItems()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTableStart("Name");
        writer.WriteTableRow("Alice");
        writer.WriteTableRow("Bob");
        writer.WriteTableRow("Carol");
        writer.WriteTableEnd();
        var output = sw.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void FailedStreamingStart_DoesNotResetTheOpenTable()
    {
        var sink = new StringWriter();
        var writer = new TableWriter(
            sink,
            (ITableFormatter)new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                MaxItems = 1
            });

        writer.WriteTableStart(["A"]);
        writer.WriteTableRow(["first"]);
        Assert.Throws<ArgumentException>(
            () => writer.WriteTableStart(["A-B", "A B"]));
        writer.WriteTableRow(["second"]);
        writer.WriteTableEnd();

        Assert.Equal("{\"a\":\"first\"}\n", sink.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void FailedFormatterBegin_DoesNotCreateAnOpenTable()
    {
        var sink = new StringWriter();
        var writer = new TableWriter(
            sink,
            (IStreamingTableFormatter)new ThrowingStartFormatter());

        Assert.Throws<InvalidOperationException>(() => writer.WriteTableStart("A"));
        writer.WriteTableRow("orphan");
        writer.WriteTableEnd();

        Assert.Equal("", sink.ToString());
    }

    [Fact]
    public void WriteListItem_RendersPlainText()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteListItem("hello");
        Assert.Equal("hello\n", sw.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void WriteArray_RendersLabelBeforeItems()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteArray("Frameworks", ["net8.0", "net10.0"]);

        Assert.Equal(
            "Frameworks:\nnet8.0\nnet10.0\n",
            sw.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void WriteHeading_Suppressed()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteHeading(1, "Title");
        Assert.Equal("", sw.ToString());
    }

    [Fact]
    public void WriteFields_BufferedAndRenderedAsTable()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteFields(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "11.0.0"));

        // Fields are rendered inline (values pipe-separated)
        var output = sw.ToString();

        Assert.Contains("System.Text.Json", output);
        Assert.Contains("11.0.0", output);
        Assert.Contains("|", output);
    }

    [Fact]
    public void WriteFieldsInline_RendersInline()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteFieldsInline(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "11.0.0"));

        var output = sw.ToString();

        // Should be rendered inline, values pipe-separated with field names
        Assert.Contains("System.Text.Json", output);
        Assert.Contains("|", output);
        Assert.Contains("11.0.0", output);
    }

    [Fact]
    public void WriteFields_FlushedOnHeading()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        writer.WriteHeading(2, "Section 1", null);
        writer.WriteFields([new("Key1", "Value1")]);
        writer.WriteHeading(2, "Section 2", null);  // Should flush buffered fields

        var output = sw.ToString();
        Assert.Contains("Value1", output);
    }

    [Fact]
    public void WriteParagraph_ReturnsFalse()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter());
        var result = writer.WriteParagraph("hello");
        // TableFormatter doesn't implement IBlockFormatter
        Assert.False(result);
        Assert.Equal("", sw.ToString());
    }

    [Fact]
    public void JsonlMode_JsonTypedValues_CoercesNumbersAndBooleans()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["Name", "Count", "Ratio", "Active"],
            ["name", "count", "ratio", "active"],
            [["Alice", "3", "0.5", "true"]]);

        var root = JsonDocument.Parse(sw.ToString()).RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("name").ValueKind);
        Assert.Equal(3, root.GetProperty("count").GetInt32());
        Assert.Equal(0.5, root.GetProperty("ratio").GetDouble());
        Assert.Equal(JsonValueKind.True, root.GetProperty("active").ValueKind);
    }

    [Fact]
    public void JsonlMode_DefaultsToStringValues()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(["Count"], ["count"], [["3"]]);

        var root = JsonDocument.Parse(sw.ToString()).RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("count").ValueKind);
    }

    [Fact]
    public void JsonlMode_NullCell_IsEmptyStringNotOmitted_ByDefault()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        // A null cell (e.g. from a direct WriteTable) must serialize as "", not be dropped.
        writer.WriteTable(["A", "B"], ["a", "b"], new List<string[]> { new[] { "x", null! } });

        var root = JsonDocument.Parse(sw.ToString()).RootElement;
        Assert.Equal("x", root.GetProperty("a").GetString());
        Assert.True(root.TryGetProperty("b", out var b));
        Assert.Equal("", b.GetString());
    }

    [Fact]
    public void JsonlMode_JsonTypedValues_PreservesLargeNumbersExactly()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        var big = "123456789012345678901234567890";
        writer.WriteTable(["Big"], ["big"], [[big]]);

        // Emitted verbatim as an (unquoted) number — no rounding through double/decimal.
        Assert.Contains("\"big\":" + big, sw.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void JsonlMode_JsonTypedValues_LeavesNonJsonNumbersAsStrings()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(
            ["A", "B", "C", "D"],
            ["a", "b", "c", "d"],
            [["007", "+7", "NaN", "1e3"]]);

        var root = JsonDocument.Parse(sw.ToString()).RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("a").ValueKind);   // leading zero
        Assert.Equal(JsonValueKind.String, root.GetProperty("b").ValueKind);   // leading '+'
        Assert.Equal(JsonValueKind.String, root.GetProperty("c").ValueKind);   // NaN
        Assert.Equal(JsonValueKind.Number, root.GetProperty("d").ValueKind);   // valid JSON exponent
    }
}
