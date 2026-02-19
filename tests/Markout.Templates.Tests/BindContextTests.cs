using Markout.Templates;

namespace Markout.Templates.Tests;

[MarkoutSerializable]
public class SecurityReport
{
    public string? Title { get; set; }
    public string? Date { get; set; }
}

[MarkoutContext(typeof(SecurityReport))]
public partial class SecurityContext : MarkoutSerializerContext
{
}

public class BindContextTests
{
    [Fact]
    public void BindingName_IsSnakeCase()
    {
        var typeInfo = SecurityContext.Default.SecurityReport;
        Assert.Equal("security_report", typeInfo.BindingName);
    }

    [Fact]
    public void GetRegisteredTypes_ReturnsAllTypes()
    {
        var types = SecurityContext.Default.GetRegisteredTypes().ToList();
        Assert.Single(types);
        Assert.Equal("security_report", types[0].BindingName);
    }

    [Fact]
    public void Bind_WithContext_UsesBindingName()
    {
        var report = new SecurityReport { Title = "CVE Report", Date = "2024-10" };
        var template = MarkoutTemplate.Parse("{{security_report}}");

        var result = template.Bind(report, SecurityContext.Default).Render();

        Assert.Contains("Title: CVE Report", result);
        Assert.Contains("Date: 2024-10", result);
    }

    [Fact]
    public void Bind_WithContext_ChainsWithOtherBinds()
    {
        var report = new SecurityReport { Title = "October Report", Date = "2024-10" };
        var template = MarkoutTemplate.Parse("Generated: {{timestamp}}\n\n{{security_report}}");

        var result = template
            .Bind("timestamp", "2024-10-08T12:00:00Z")
            .Bind(report, SecurityContext.Default)
            .Render();

        Assert.Contains("Generated: 2024-10-08T12:00:00Z", result);
        Assert.Contains("Title: October Report", result);
    }
}
