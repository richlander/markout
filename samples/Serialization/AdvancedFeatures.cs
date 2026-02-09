using Markout;

namespace Markout.Samples.Serialization;

#region SkipNullAndDisplayFormat
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class ServerView
{
    public string Name { get; set; } = "";

    [MarkoutSkipNull]
    public string? Region { get; set; }

    [MarkoutDisplayFormat("{0:N0} req/s")]
    public long RequestsPerSecond { get; set; }

    [MarkoutSkipNull]
    [MarkoutDisplayFormat("{0:P1}")]
    public double? CpuUsage { get; set; }

    [MarkoutSkipDefault]
    public bool MaintenanceMode { get; set; }
}
#endregion

#region ConditionalSections
[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class AuditReport
{
    [MarkoutIgnore]
    public string? Title { get; set; }

    public bool HasWarnings { get; set; }
    public bool HasErrors { get; set; }

    [MarkoutSection(Name = "Warnings", ShowWhenProperty = nameof(HasWarnings))]
    public List<string>? Warnings { get; set; }

    [MarkoutSection(Name = "Errors", ShowWhenProperty = nameof(HasErrors))]
    public List<string>? Errors { get; set; }

    [MarkoutSection(Name = "Files")]
    [MarkoutMaxItems(5)]
    public List<string>? Files { get; set; }
}
#endregion

#region TableDisplay
[MarkoutSerializable]
public class MetricRow
{
    public string Name { get; set; } = "";

    [MarkoutTableDisplay("{0:N0} req")]
    public long Requests { get; set; }

    [MarkoutTableDisplay("{0:P0}")]
    public double ErrorRate { get; set; }
}
#endregion

[MarkoutContext(typeof(ServerView))]
[MarkoutContext(typeof(AuditReport))]
[MarkoutContext(typeof(MetricRow))]
public partial class AdvancedContext : MarkoutSerializerContext { }

/// <summary>
/// Demonstrates the new expressiveness features: SkipNull, DisplayFormat,
/// ShowWhenProperty, MaxItems, and TableDisplay.
/// </summary>
public static class AdvancedFeatures
{
    /// <summary>
    /// Shows SkipNull and DisplayFormat on scalar fields.
    /// </summary>
    public static void SkipNullAndDisplayFormat()
    {
        #region SkipNullAndDisplayFormatUsage
        var server = new ServerView
        {
            Name = "prod-web-01",
            Region = "us-east-1",
            RequestsPerSecond = 12500,
            CpuUsage = 0.73,
            MaintenanceMode = false // skipped because [MarkoutSkipDefault]
        };

        string markdown = MarkoutSerializer.Serialize(server, AdvancedContext.Default);
        // # prod-web-01
        //
        // Region: us-east-1 | RequestsPerSecond: 12,500 req/s | CpuUsage: 73.0 %
        #endregion

        Console.WriteLine(markdown);
        Console.WriteLine();

        // With nulls — Region and CpuUsage are omitted
        var minimal = new ServerView
        {
            Name = "staging-01",
            RequestsPerSecond = 50,
            Region = null,
            CpuUsage = null
        };
        Console.WriteLine(MarkoutSerializer.Serialize(minimal, AdvancedContext.Default));
    }

    /// <summary>
    /// Shows conditional sections and MaxItems truncation.
    /// </summary>
    public static void ConditionalSectionsAndMaxItems()
    {
        #region ConditionalSectionsUsage
        var report = new AuditReport
        {
            Title = "Build Audit",
            HasWarnings = true,
            HasErrors = false,
            Warnings = new() { "Deprecated API usage in Foo.cs", "Missing XML docs on Bar.cs" },
            Errors = new() { "This won't show because HasErrors is false" },
            Files = new() { "Foo.cs", "Bar.cs", "Baz.cs", "Qux.cs", "Quux.cs", "Corge.cs", "Grault.cs" }
        };

        string markdown = MarkoutSerializer.Serialize(report, AdvancedContext.Default);
        // Warnings section appears, Errors section is hidden
        // Files section shows first 5 with "... and 2 more"
        #endregion

        Console.WriteLine(markdown);
    }
}
