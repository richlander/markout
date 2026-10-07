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

    [Fact]
    public void RawValueConverterWhitespaceStillProducesOnePhysicalLinePerRecord()
    {
        var output = new StringWriter { NewLine = "\n" };

        JsonLinesWriter.WriteRows(
            output,
            [new RawJsonLineTestRow("1.0.0")],
            JsonLineTestContext.Default.RawJsonLineTestRow);

        Assert.Equal("{  \"raw_version\": \"1.0.0\"}\n", output.ToString());
    }
}

internal sealed record JsonLineTestRow(string Name, int Count, string? Optional);

[JsonConverter(typeof(RawJsonLineTestRowConverter))]
internal sealed record RawJsonLineTestRow(string Version);

internal sealed class RawJsonLineTestRowConverter :
    JsonConverter<RawJsonLineTestRow>
{
    public override RawJsonLineTestRow Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(
        Utf8JsonWriter writer,
        RawJsonLineTestRow value,
        JsonSerializerOptions options) =>
        writer.WriteRawValue(
            $$"""
            {
              "raw_version": "{{value.Version}}"
            }
            """);
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonLineTestRow))]
[JsonSerializable(typeof(RawJsonLineTestRow), GenerationMode = JsonSourceGenerationMode.Metadata)]
internal partial class JsonLineTestContext : JsonSerializerContext;
