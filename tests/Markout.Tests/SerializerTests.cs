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

        var context = new SectionTestContext(new MarkoutWriterOptions { IncludeSections = ["Dependencies"] });
        var mdf = context.Serialize(package);

        // Dependencies section should be included
        Assert.Contains("## Dependencies", mdf);
        Assert.Contains("Dep1", mdf);

        // Assemblies section should be excluded
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

        var context = new SectionTestContext(new MarkoutWriterOptions { ExcludeSections = ["Dependencies"] });
        var mdf = context.Serialize(package);

        // Dependencies section should be excluded
        Assert.DoesNotContain("## Dependencies", mdf);
        Assert.DoesNotContain("Dep1", mdf);

        // Assemblies section should be included
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

    [Fact]
    public void Serialize_RenderScalarsFalseWithNoSections_ProducesEmptyOutput()
    {
        // This type has RenderScalars=false but no sections - output will be empty
        // The MARKOUT004 warning is suppressed with #pragma in the type definition
        var data = new RenderScalarsWarningTest { Name = "Test", Count = 42 };

        var context = new TreeTestContext();
        var mdf = context.Serialize(data);

        // Output should be empty (no scalars rendered, no sections)
        Assert.Equal("", mdf.Trim());
    }

    [Fact]
    public void Context_DefaultInstance_HasReadOnlyOptions()
    {
        var context = TestMarkoutContext.Default;

        Assert.True(context.Options.IsReadOnly);
    }

    [Fact]
    public void Context_ParameterlessConstructor_FreezesOptions()
    {
        var context = new TestMarkoutContext();

        Assert.True(context.Options.IsReadOnly);
    }

    [Fact]
    public void Context_WithOptions_BindsAndFreezesOptions()
    {
        var options = new MarkoutWriterOptions { BoldFieldNames = true };
        var context = new TestMarkoutContext(options);

        Assert.True(context.Options.IsReadOnly);
        Assert.True(context.Options.BoldFieldNames);
        Assert.Same(options, context.Options);
    }

    [Fact]
    public void Context_WithOptions_OptionsCannotBeMutatedAfterBinding()
    {
        var options = new MarkoutWriterOptions { BoldFieldNames = true };
        var context = new TestMarkoutContext(options);

        Assert.Throws<InvalidOperationException>(() => options.BoldFieldNames = false);
    }

    [Fact]
    public void Context_WithReadOnlyOptions_Throws()
    {
        var options = new MarkoutWriterOptions();
        options.MakeReadOnly();

        Assert.Throws<InvalidOperationException>(() => new TestMarkoutContext(options));
    }

    [Fact]
    public void Context_OptionsPropertyHasNoSetter()
    {
        // Verify that Options is get-only (no set accessor)
        var prop = typeof(MarkoutSerializerContext).GetProperty("Options");
        Assert.NotNull(prop);
        Assert.NotNull(prop!.GetMethod);
        Assert.Null(prop.SetMethod);
    }

    [Fact]
    public void Serialize_EnumProperty_RendersEnumName()
    {
        var task = new TaskItem { Name = "Build", Priority = Priority.High };
        var mdf = MarkoutSerializer.Serialize(task, EnumTestContext.Default);

        Assert.Contains("Priority: High", mdf);
        Assert.Contains("Name: Build", mdf);
    }

    [Fact]
    public void Serialize_NullableEnum_SuppressesWhenNull()
    {
        var task = new TaskItem { Name = "Build", Priority = Priority.Low, OptionalPriority = null };
        var mdf = MarkoutSerializer.Serialize(task, EnumTestContext.Default);

        Assert.Contains("Priority: Low", mdf);
        Assert.DoesNotContain("OptionalPriority", mdf);
    }

    [Fact]
    public void Serialize_NullableEnum_RendersWhenSet()
    {
        var task = new TaskItem { Name = "Build", Priority = Priority.Low, OptionalPriority = Priority.Critical };
        var mdf = MarkoutSerializer.Serialize(task, EnumTestContext.Default);

        Assert.Contains("Critical", mdf);
    }

    [Fact]
    public void Serialize_JoinedList_RendersAsJoinedField()
    {
        var project = new ProjectInfo
        {
            Name = "MyLib",
            Tags = ["api", "web", "tools"],
            Frameworks = ["net8.0", "net9.0"]
        };
        var mdf = MarkoutSerializer.Serialize(project, JoinTestContext.Default);

        Assert.Contains("Tags: api, web, tools", mdf);
        Assert.Contains("Frameworks: net8.0 | net9.0", mdf);
        Assert.DoesNotContain("- api", mdf); // Should NOT be a bullet list
    }

    [Fact]
    public void Serialize_JoinedList_SuppressesWhenNull()
    {
        var project = new ProjectInfo { Name = "MyLib", Tags = null, Frameworks = null };
        var mdf = MarkoutSerializer.Serialize(project, JoinTestContext.Default);

        Assert.Contains("Name: MyLib", mdf);
        Assert.DoesNotContain("Tags", mdf);
        Assert.DoesNotContain("Frameworks", mdf);
    }

    [Fact]
    public void Serialize_JoinedList_SuppressesWhenEmpty()
    {
        var project = new ProjectInfo { Name = "MyLib", Tags = [], Frameworks = [] };
        var mdf = MarkoutSerializer.Serialize(project, JoinTestContext.Default);

        Assert.Contains("Name: MyLib", mdf);
        Assert.DoesNotContain("Tags", mdf);
        Assert.DoesNotContain("Frameworks", mdf);
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
    [MarkoutIgnoreInTable]
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
[MarkoutContext(typeof(RenderScalarsWarningTest))]
public partial class TreeTestContext : MarkoutSerializerContext
{
}

// Test type that intentionally triggers MARKOUT003 error for dictionary properties
// This verifies the analyzer correctly detects Dictionary<TKey, TValue> usage
#pragma warning disable MARKOUT003, MARKOUT001
[MarkoutSerializable]
public class DictionaryWarningTest
{
    public string Name { get; set; } = "";
    public Dictionary<string, string> Tags { get; set; } = new();
}
#pragma warning restore MARKOUT003, MARKOUT001

[MarkoutContext(typeof(DictionaryWarningTest))]
public partial class DictionaryTestContext : MarkoutSerializerContext
{
}

// Test type that intentionally triggers MARKOUT004 warning
// This verifies the analyzer correctly warns when RenderScalars=false but no sections exist
#pragma warning disable MARKOUT004
[MarkoutSerializable(RenderScalars = false)]
public class RenderScalarsWarningTest
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    // No [MarkoutSection] or FieldCollection properties - output will be empty
}
#pragma warning restore MARKOUT004

// Enum support types
public enum Priority { Low, Medium, High, Critical }

[MarkoutSerializable]
public class TaskItem
{
    public string? Name { get; set; }
    public Priority Priority { get; set; }
    public Priority? OptionalPriority { get; set; }
}

[MarkoutContext(typeof(TaskItem))]
public partial class EnumTestContext : MarkoutSerializerContext
{
}

// Collection joining types
[MarkoutSerializable]
public class ProjectInfo
{
    public string? Name { get; set; }
    [MarkoutJoin(", ")]
    public List<string>? Tags { get; set; }
    [MarkoutJoin(" | ")]
    public string[]? Frameworks { get; set; }
}

[MarkoutContext(typeof(ProjectInfo))]
public partial class JoinTestContext : MarkoutSerializerContext
{
}
