using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Markout.Tests;

public class JsonLinesWriterTests
{
    [Fact]
    public void TypedRowsReachDestinationBeforeNextRowIsRequested()
    {
        var output = new StringWriter { NewLine = "\n" };

        IEnumerable<JsonLineTestRow> Rows()
        {
            yield return new JsonLineTestRow("first", 1, null);
            Assert.Equal("{\"name\":\"first\",\"count\":1}\n", output.ToString());
            yield return new JsonLineTestRow("second\nline", 2, "value");
        }

        JsonLinesWriter.WriteRows(output, Rows(), JsonLineTestContext.Default.JsonLineTestRow);

        Assert.Equal(
            "{\"name\":\"first\",\"count\":1}\n"
            + "{\"name\":\"second\\nline\",\"count\":2,\"optional\":\"value\"}\n",
            output.ToString());
    }

    [Fact]
    public void TypedRowsUseTheProvidedSerializerMetadata()
    {
        JsonLineTestRow row = new("<name>", 3, null);
        var output = new StringWriter { NewLine = "\n" };

        JsonLinesWriter.WriteRows(output, [row], JsonLineTestContext.Default.JsonLineTestRow);

        Assert.Equal(
            JsonSerializer.Serialize(row, JsonLineTestContext.Default.JsonLineTestRow) + "\n",
            output.ToString());
    }

    [Fact]
    public void IndentedMetadataStillProducesOnePhysicalLinePerRecord()
    {
        var context = new JsonLineTestContext(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });
        var output = new StringWriter { NewLine = "\n" };

        JsonLinesWriter.WriteRows(output,
            [new JsonLineTestRow("first", 1, null)], context.JsonLineTestRow);

        Assert.Equal("{\"name\":\"first\",\"count\":1,\"optional\":null}\n", output.ToString());
    }
}

internal sealed record JsonLineTestRow(string Name, int Count, string? Optional);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonLineTestRow))]
internal partial class JsonLineTestContext : JsonSerializerContext;
