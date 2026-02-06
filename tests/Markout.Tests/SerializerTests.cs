using Markout;
using Xunit;

namespace Markout.Tests;

[MarkoutSerializable]
public class Package
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public bool Signed { get; set; }
    [MarkoutIgnoreInTable]
    public List<string>? Frameworks { get; set; }
    [MarkoutIgnoreInTable]
    public List<Assembly>? Assemblies { get; set; }
}

[MarkoutSerializable]
public class Assembly
{
    public string? File { get; set; }
    public string? Arch { get; set; }
    public bool Signed { get; set; }
    public bool Deterministic { get; set; }
}

[MarkoutSerializable]
public class SimpleRecord
{
    public string? Title { get; set; }

    [MarkoutPropertyName("Display Name")]
    public string? Name { get; set; }

    [MarkoutIgnore]
    public string? Secret { get; set; }

    public int Count { get; set; }
}

[MarkoutContext(typeof(Package))]
[MarkoutContext(typeof(SimpleRecord))]
public partial class TestMarkoutContext : MarkoutSerializerContext
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

        var mdf = MarkoutSerializer.Serialize(record, TestMarkoutContext.Default);

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

        var mdf = MarkoutSerializer.Serialize(package, TestMarkoutContext.Default);

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

        var mdf = MarkoutSerializer.Serialize(package, TestMarkoutContext.Default);

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

        var mdf = MarkoutSerializer.Serialize(package, TestMarkoutContext.Default);

        // Should have a table
        Assert.Contains("| File |", mdf);
        Assert.Contains("| Foo.dll |", mdf);
        Assert.Contains("| Bar.dll |", mdf);
    }

    [Fact]
    public void Serialize_WithContext_Default()
    {
        var record = new SimpleRecord { Title = "Hello" };

        var mdf = TestMarkoutContext.Default.Serialize(record);

        Assert.Contains("Title: Hello", mdf);
    }

    [Fact]
    public void Serialize_UnregisteredType_ThrowsException()
    {
        var context = TestMarkoutContext.Default;

        Assert.Throws<InvalidOperationException>(() =>
            context.Serialize(new object()));
    }

    [Fact]
    public void Serialize_WithIncludeSections_OnlyRendersSpecifiedSections()
    {
        var package = new PackageWithSections
        {
            Name = "TestPackage",
            Version = "1.0.0",
            Dependencies = new List<SimpleDep>
            {
                new() { Id = "Dep1", Version = "1.0" }
            },
            Assemblies = new List<SimpleAsm>
            {
                new() { Name = "Test.dll", Arch = "x64" }
            }
        };

        var context = new SectionTestContext { IncludeSections = new HashSet<int> { 1 } };
        var mdf = context.Serialize(package);

        // Section 1 (Dependencies) should be included
        Assert.Contains("## Dependencies", mdf);
        Assert.Contains("Dep1", mdf);

        // Section 2 (Assemblies) should be excluded
        Assert.DoesNotContain("## Assemblies", mdf);
        Assert.DoesNotContain("Test.dll", mdf);
    }

    [Fact]
    public void Serialize_WithExcludeSections_SkipsSpecifiedSections()
    {
        var package = new PackageWithSections
        {
            Name = "TestPackage",
            Version = "1.0.0",
            Dependencies = new List<SimpleDep>
            {
                new() { Id = "Dep1", Version = "1.0" }
            },
            Assemblies = new List<SimpleAsm>
            {
                new() { Name = "Test.dll", Arch = "x64" }
            }
        };

        var context = new SectionTestContext { ExcludeSections = new HashSet<int> { 1 } };
        var mdf = context.Serialize(package);

        // Section 1 (Dependencies) should be excluded
        Assert.DoesNotContain("## Dependencies", mdf);
        Assert.DoesNotContain("Dep1", mdf);

        // Section 2 (Assemblies) should be included
        Assert.Contains("## Assemblies", mdf);
        Assert.Contains("Test.dll", mdf);
    }

    [Fact]
    public void Serialize_WithTitleContextProperty_RendersTitleWithContext()
    {
        var package = new PackageWithTitleContext
        {
            Name = "Newtonsoft.Json",
            Version = "13.0.3",
            Description = "Popular JSON library"
        };

        var context = new SectionTestContext();
        var mdf = context.Serialize(package);

        // Should have title with version in parentheses
        Assert.Contains("# Newtonsoft.Json (13.0.3)", mdf);
    }

    [Fact]
    public void Serialize_WithBoolFormatAttribute_UsesCustomTrueFalse()
    {
        var audit = new AuditRecord
        {
            Name = "Test.dll",
            IsDeterministic = true,
            HasSourceLink = false
        };

        var context = new BoolFormatTestContext();
        var mdf = context.Serialize(audit);

        // Should use custom symbols instead of yes/no
        Assert.Contains("IsDeterministic: ✓", mdf);
        Assert.Contains("HasSourceLink: ✗", mdf);
    }

    [Fact]
    public void Serialize_WithBoolFormatInTable_UsesCustomSymbols()
    {
        var report = new AuditReport
        {
            Title = "Build Audit",
            Audits = new List<AuditRecord>
            {
                new() { Name = "Foo.dll", IsDeterministic = true, HasSourceLink = true },
                new() { Name = "Bar.dll", IsDeterministic = false, HasSourceLink = false }
            }
        };

        var context = new BoolFormatTestContext();
        var mdf = context.Serialize(report);

        // Table should use custom symbols
        Assert.Contains("| ✓ |", mdf);
        Assert.Contains("| ✗ |", mdf);
    }

    [Fact]
    public void Serialize_WithTreeProperty_RendersTree()
    {
        var typeShape = new TypeShape
        {
            Name = "MyClass",
            Kind = "class",
            Members = new List<TreeNode>
            {
                new("Inherits", new[] { "BaseClass" }),
                new("Properties (2)", new[] { "string Name", "int Count" })
            }
        };

        var context = new TreeTestContext();
        var mdf = context.Serialize(typeShape);

        // Should render tree structure
        Assert.Contains("# MyClass", mdf);
        Assert.Contains("Kind: class", mdf);
        Assert.Contains("Inherits", mdf);
        Assert.Contains("BaseClass", mdf);
        Assert.Contains("Properties (2)", mdf);
        Assert.Contains("string Name", mdf);
        // Tree uses box-drawing characters
        Assert.Contains("├─", mdf);
        Assert.Contains("└─", mdf);
    }

    [Fact]
    public void Serialize_WithTreeSection_RendersHeadingAndTree()
    {
        var explorer = new FileExplorer
        {
            Title = "Package Contents",
            Files = new List<TreeNode>
            {
                new("lib", new List<TreeNode>
                {
                    new("net8.0", new[] { "MyLib.dll" }),
                    new("net9.0", new[] { "MyLib.dll" })
                })
            }
        };

        var context = new TreeTestContext();
        var mdf = context.Serialize(explorer);

        // Should have section heading
        Assert.Contains("# Package Contents", mdf);
        Assert.Contains("## Files", mdf);
        Assert.Contains("lib", mdf);
        Assert.Contains("net8.0", mdf);
        Assert.Contains("MyLib.dll", mdf);
    }
}

[MarkoutSerializable]
public class PackageWithSections
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";

    [MarkoutSection(Name = "Dependencies")]
    public List<SimpleDep>? Dependencies { get; set; }

    [MarkoutSection(Name = "Assemblies")]
    public List<SimpleAsm>? Assemblies { get; set; }
}

[MarkoutSerializable]
public class SimpleDep
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
}

[MarkoutSerializable]
public class SimpleAsm
{
    public string Name { get; set; } = "";
    public string Arch { get; set; } = "";
}

[MarkoutContext(typeof(PackageWithSections))]
[MarkoutContext(typeof(PackageWithTitleContext))]
public partial class SectionTestContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(TitleProperty = nameof(Name), TitleContextProperty = nameof(Version))]
public class PackageWithTitleContext
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string? Description { get; set; }
}

[MarkoutSerializable]
public class AuditRecord
{
    public string Name { get; set; } = "";

    [MarkoutBoolFormat("✓", "✗")]
    public bool IsDeterministic { get; set; }

    [MarkoutBoolFormat("✓", "✗")]
    public bool HasSourceLink { get; set; }
}

[MarkoutSerializable]
public class AuditReport
{
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Audits")]
    public List<AuditRecord>? Audits { get; set; }
}

[MarkoutContext(typeof(AuditRecord))]
[MarkoutContext(typeof(AuditReport))]
public partial class BoolFormatTestContext : MarkoutSerializerContext
{
}

// Tree serialization test types
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class TypeShape
{
    [MarkoutIgnore]
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public List<TreeNode> Members { get; set; } = [];
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class FileExplorer
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";
    
    [MarkoutSection(Name = "Files")]
    public List<TreeNode>? Files { get; set; }
}

[MarkoutContext(typeof(TypeShape))]
[MarkoutContext(typeof(FileExplorer))]
public partial class TreeTestContext : MarkoutSerializerContext
{
}
