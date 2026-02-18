using System.Text;
using MarkdownTable.Formatting;
using Markout.Templates;

namespace Markout.Templates.Tests;

public class BindFieldsTests
{
    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void BindFields_ScalarField_ResolvedInHeading()
    {
        var fields = Utf8("packageName: Newtonsoft.Json\nversion: 13.0.3");
        var template = MarkoutTemplate.Parse("# {{packageName}} ({{version}})");

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.Equal("# Newtonsoft.Json (13.0.3)", result);
    }

    [Fact]
    public void BindFields_ScalarField_ResolvedInParagraph()
    {
        var fields = Utf8("description: A high-performance JSON framework");
        var template = MarkoutTemplate.Parse("{{description}}");

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.Contains("A high-performance JSON framework", result);
    }

    [Fact]
    public void BindFields_ArrayField_JoinedWithComma()
    {
        var fields = Utf8("targetFrameworks:\n- net6.0\n- net8.0\n- net9.0");
        var template = MarkoutTemplate.Parse("Frameworks: {{targetFrameworks}}");

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.Equal("Frameworks: net6.0, net8.0, net9.0", result);
    }

    [Fact]
    public void BindFields_ConditionalTrue_RendersContent()
    {
        var fields = Utf8("hasReadme: true\nreadmeFile: README.md");
        var template = MarkoutTemplate.Parse(
            "{{#if hasReadme}}\nReadme: {{readmeFile}}\n{{/if}}");

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.Contains("Readme: README.md", result);
    }

    [Fact]
    public void BindFields_ConditionalMissingKey_SkipsContent()
    {
        var fields = Utf8("packageName: Test");
        var template = MarkoutTemplate.Parse(
            "{{#if missingKey}}\nShould not appear\n{{/if}}\n\nEnd");
        template.SkipUnboundPlaceholders = true;

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.DoesNotContain("Should not appear", result);
        Assert.Contains("End", result);
    }

    [Fact]
    public void BindFields_BoldFieldFormat_ParsedCorrectly()
    {
        var fields = Utf8("**packageName:** Newtonsoft.Json\n**version:** 13.0.3");
        var template = MarkoutTemplate.Parse("# {{packageName}} v{{version}}");

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.Equal("# Newtonsoft.Json v13.0.3", result);
    }

    [Fact]
    public void BindFields_FieldDocumentOverload_WorksIdentically()
    {
        var bytes = Utf8("name: Alice\nage: 30");
        using var doc = FieldDocument.Parse(bytes);
        var template = MarkoutTemplate.Parse("{{name}} is {{age}}");

        var result = template.BindFields(doc).Render().TrimEnd();

        Assert.Equal("Alice is 30", result);
    }

    [Fact]
    public void BindFields_CaseInsensitiveKeys()
    {
        var fields = Utf8("PackageName: Test");
        var template = MarkoutTemplate.Parse("{{packagename}}");

        var result = template.BindFields(fields).Render().TrimEnd();

        Assert.Contains("Test", result);
    }

    [Fact]
    public void BindFields_ChainingWithExplicitBinds()
    {
        var fields = Utf8("packageName: TestPkg");
        var template = MarkoutTemplate.Parse("# {{packageName}} — {{extra}}");

        var result = template
            .BindFields(fields)
            .Bind("extra", "custom value")
            .Render()
            .TrimEnd();

        Assert.Equal("# TestPkg — custom value", result);
    }

    [Fact]
    public void BindFields_FullCacheDocument_RendersTemplate()
    {
        var cacheFile = Utf8(
            "packageName: Newtonsoft.Json\n" +
            "version: 13.0.3\n" +
            "description: Json.NET is a popular high-performance JSON framework\n" +
            "authors: James Newton-King\n" +
            "license: MIT\n" +
            "assemblyCount: 4\n" +
            "hasReadme: true\n" +
            "\n" +
            "targetFrameworks:\n" +
            "- net6.0\n" +
            "- netstandard2.0\n");

        var template = MarkoutTemplate.Parse(
            "# {{packageName}} ({{version}})\n" +
            "\n" +
            "{{description}}\n" +
            "\n" +
            "| Property | Value |\n" +
            "| -------- | ----- |\n" +
            "| Authors | {{authors}} |\n" +
            "| License | {{license}} |\n" +
            "| Frameworks | {{targetFrameworks}} |");

        var result = template.BindFields(cacheFile).Render();

        Assert.Contains("# Newtonsoft.Json (13.0.3)", result);
        Assert.Contains("Json.NET is a popular", result);
        Assert.Contains("James Newton-King", result);
        Assert.Contains("MIT", result);
        Assert.Contains("net6.0, netstandard2.0", result);
    }
}
