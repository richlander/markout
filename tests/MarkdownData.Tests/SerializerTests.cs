using MarkdownData;
using Xunit;

namespace MarkdownData.Tests;

[MdfSerializable]
public class Package
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public bool Signed { get; set; }
    public List<string>? Frameworks { get; set; }
    public List<Assembly>? Assemblies { get; set; }
}

[MdfSerializable]
public class Assembly
{
    public string? File { get; set; }
    public string? Arch { get; set; }
    public bool Signed { get; set; }
    public bool Deterministic { get; set; }
}

[MdfSerializable]
public class SimpleRecord
{
    public string? Title { get; set; }

    [MdfPropertyName("Display Name")]
    public string? Name { get; set; }

    [MdfIgnore]
    public string? Secret { get; set; }

    public int Count { get; set; }
}

[MdfContext(typeof(Package))]
[MdfContext(typeof(SimpleRecord))]
public partial class TestMdfContext : MdfSerializerContext
{
}

public class SerializerTests
{
    [Fact]
    public void Serialize_SimpleRecord_UsesCustomPropertyName()
    {
        var record = new SimpleRecord
        {
            Title = "My Title",
            Name = "Test Name",
            Secret = "Should be ignored",
            Count = 42
        };

        var mdf = MdfSerializer.Serialize(record, TestMdfContext.Default);

        Assert.Contains("Title: My Title", mdf);
        Assert.Contains("Display Name: Test Name", mdf);
        Assert.Contains("Count: 42", mdf);
        Assert.DoesNotContain("Secret", mdf);
        Assert.DoesNotContain("Should be ignored", mdf);
    }

    [Fact]
    public void Serialize_Package_WithScalarFields()
    {
        var package = new Package
        {
            Name = "Newtonsoft.Json",
            Version = "13.0.3",
            Signed = true
        };

        var mdf = MdfSerializer.Serialize(package, TestMdfContext.Default);

        Assert.Contains("Name: Newtonsoft.Json", mdf);
        Assert.Contains("Version: 13.0.3", mdf);
        Assert.Contains("Signed: yes", mdf);
    }

    [Fact]
    public void Serialize_Package_WithStringArray()
    {
        var package = new Package
        {
            Name = "Test",
            Frameworks = new List<string> { "netstandard2.0", "net6.0", "net8.0" }
        };

        var mdf = MdfSerializer.Serialize(package, TestMdfContext.Default);

        Assert.Contains("Frameworks:", mdf);
        Assert.Contains("- netstandard2.0", mdf);
        Assert.Contains("- net6.0", mdf);
        Assert.Contains("- net8.0", mdf);
    }

    [Fact]
    public void Serialize_Package_WithComplexArray()
    {
        var package = new Package
        {
            Name = "Test",
            Assemblies = new List<Assembly>
            {
                new Assembly { File = "Foo.dll", Arch = "AnyCPU", Signed = true, Deterministic = true },
                new Assembly { File = "Bar.dll", Arch = "x64", Signed = false, Deterministic = false }
            }
        };

        var mdf = MdfSerializer.Serialize(package, TestMdfContext.Default);

        // Should have a table
        Assert.Contains("| File |", mdf);
        Assert.Contains("| Foo.dll |", mdf);
        Assert.Contains("| Bar.dll |", mdf);
    }

    [Fact]
    public void Serialize_WithContext_Default()
    {
        var record = new SimpleRecord { Title = "Hello" };

        var mdf = TestMdfContext.Default.Serialize(record);

        Assert.Contains("Title: Hello", mdf);
    }

    [Fact]
    public void Serialize_UnregisteredType_ThrowsException()
    {
        var context = TestMdfContext.Default;

        Assert.Throws<InvalidOperationException>(() =>
            context.Serialize(new object()));
    }
}
