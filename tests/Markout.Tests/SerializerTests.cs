using Markout;

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

[MarkoutSerializable]
public class HeaderCallbackContainer
{
    [MarkoutIgnoreInTable]
    public List<HeaderCallbackRow>? Rows { get; set; }
}

[MarkoutSerializable]
public class HeaderCallbackRow
{
    [MarkoutPropertyName("Return Type")]
    public string? ReturnType { get; set; }
    public string? Sim { get; set; }
}

[MarkoutContext(typeof(HeaderCallbackContainer))]
public partial class HeaderCallbackContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(AutoFields = false)]
public class FieldOrderContainer
{
    [MarkoutSection(Name = "Package Info", FieldOrder = MarkoutFieldOrder.Alphabetical)]
    public List<MarkoutField>? PackageInfo { get; set; }
}

[MarkoutSerializable(AutoFields = false)]
public class ScalarFieldOrderContainer
{
    [MarkoutSection(Name = "Package Info", FieldOrder = MarkoutFieldOrder.Alphabetical)]
    public string? Version { get; set; }

    [MarkoutSection(Name = "Package Info", FieldOrder = MarkoutFieldOrder.Alphabetical)]
    public string? Authors { get; set; }

    [MarkoutSection(Name = "Package Info", FieldOrder = MarkoutFieldOrder.Alphabetical)]
    public string? License { get; set; }
}

[MarkoutContext(typeof(FieldOrderContainer))]
[MarkoutContext(typeof(ScalarFieldOrderContainer))]
public partial class FieldOrderContext : MarkoutSerializerContext
{
}

[MarkoutSerializable]
public class BoldRecord
{
    public string? Label { get; set; }
    public int Value { get; set; }
}

[MarkoutContextOptions(BoldFieldNames = true)]
[MarkoutContext(typeof(BoldRecord))]
public partial class BoldContext : MarkoutSerializerContext
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

        Assert.Contains("| Title | My Title |", mdf);
        Assert.Contains("| Display Name | Test Name |", mdf);
        Assert.Contains("| Count | 42 |", mdf);
        Assert.DoesNotContain("Secret", mdf);
        Assert.DoesNotContain("Should be ignored", mdf);
    }

    [Fact]
    public void Serialize_StringOverloads_UseConfiguredNewLine()
    {
        var record = new SimpleRecord { Name = "Test", Count = 1 };
        var options = new MarkoutWriterOptions { NewLine = "<NL>" };

        var fromContext = MarkoutSerializer.Serialize(record, TestMarkoutContext.Default, options);
        var typeInfo = TestMarkoutContext.Default.GetTypeInfo<SimpleRecord>();
        var fromTypeInfo = MarkoutSerializer.Serialize(record, typeInfo!, options);

        Assert.Contains("<NL>", fromContext, StringComparison.Ordinal);
        Assert.Contains("<NL>", fromTypeInfo, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.NewLine, fromContext, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.NewLine, fromTypeInfo, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_CallerSuppliedTextWriter_KeepsItsOwnNewLine()
    {
        var record = new SimpleRecord { Name = "Test", Count = 1 };
        var output = new StringWriter { NewLine = "<WRITER>" };
        var options = new MarkoutWriterOptions { NewLine = "<OPTIONS>" };

        MarkoutSerializer.Serialize(record, output, TestMarkoutContext.Default, options);

        Assert.Contains("<WRITER>", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("<OPTIONS>", output.ToString(), StringComparison.Ordinal);
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

        Assert.Contains("| Name | Newtonsoft.Json |", mdf);
        Assert.Contains("| Version | 13.0.3 |", mdf);
        Assert.Contains("| Signed | yes |", mdf);
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
    public void Serialize_TableHeaderFormatter_ReceivesGeneratedPropertyNamesAndDisplayNames()
    {
        var data = new HeaderCallbackContainer
        {
            Rows =
            [
                new() { ReturnType = "string", Sim = "0.50" }
            ]
        };
        var context = new HeaderCallbackContext(new MarkoutWriterOptions
        {
            FormatTableHeader = header => $"{header.Name}:{header.DisplayName}:{header.Index}"
        });

        var mdf = context.Serialize(data);

        Assert.Contains("| ReturnType:Return Type:0 | Sim:Sim:1 |", mdf);
        Assert.Contains("| string | 0.50 |", mdf);
    }

    [Fact]
    public void Serialize_FieldCollectionSection_CanOrderFieldsAlphabetically()
    {
        var data = new FieldOrderContainer
        {
            PackageInfo =
            [
                new("Version", "1.0.0"),
                new("Authors", "tests"),
                new("License", "MIT")
            ]
        };

        var mdf = MarkoutSerializer.Serialize(data, FieldOrderContext.Default);

        Assert.True(
            mdf.IndexOf("| Authors |", StringComparison.Ordinal) < mdf.IndexOf("| License |", StringComparison.Ordinal)
            && mdf.IndexOf("| License |", StringComparison.Ordinal) < mdf.IndexOf("| Version |", StringComparison.Ordinal),
            mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_CanOrderFieldsAlphabetically()
    {
        var data = new ScalarFieldOrderContainer
        {
            Version = "1.0.0",
            Authors = "tests",
            License = "MIT"
        };

        var mdf = MarkoutSerializer.Serialize(data, FieldOrderContext.Default);

        Assert.True(
            mdf.IndexOf("| Authors |", StringComparison.Ordinal) < mdf.IndexOf("| License |", StringComparison.Ordinal)
            && mdf.IndexOf("| License |", StringComparison.Ordinal) < mdf.IndexOf("| Version |", StringComparison.Ordinal),
            mdf);
    }

    [Fact]
    public void Serialize_WithContext_Default()
    {
        var record = new SimpleRecord { Title = "Hello" };

        var mdf = TestMarkoutContext.Default.Serialize(record);

        Assert.Contains("| Title | Hello |", mdf);
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
    public void Serialize_WithIncludeSections_SkipsUnspecifiedSections()
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

        var context = new SectionTestContext(new MarkoutWriterOptions { IncludeSections = ["Assemblies"] });
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
        Assert.Contains("| Is Deterministic | ✓ |", mdf);
        Assert.Contains("| Has Source Link | ✗ |", mdf);
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
            Members = [
                new TreeNode("Inherits", [new TreeNode("BaseClass")]),
                new TreeNode("Properties (2)", [new TreeNode("string Name"), new TreeNode("int Count")])
            ]
        };

        var context = new TreeTestContext();
        var mdf = context.Serialize(typeShape);

        // Should render tree structure
        Assert.Contains("# MyClass", mdf);
        Assert.Contains("| Kind | class |", mdf);
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
            Files = [
                new TreeNode("lib", [
                    new TreeNode("net8.0", [new TreeNode("MyLib.dll")]),
                    new TreeNode("net9.0", [new TreeNode("MyLib.dll")])])
            ]
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
    public void Serialize_AutoFieldsFalseWithNoSections_ProducesEmptyOutput()
    {
        // This type has AutoFields=false but no sections - output will be empty
        // The MARKOUT004 warning is suppressed with #pragma in the type definition
        var data = new AutoFieldsWarningTest { Name = "Test", Count = 42 };

        var context = new TreeTestContext();
        var mdf = context.Serialize(data);

        // Output should be empty (no scalars rendered, no sections)
        Assert.Equal("", mdf.Trim());
    }

    [Fact]
    public void Serialize_AutoFieldsCount_OnlyRendersFirstNScalars()
    {
        var data = new AutoFieldsCountTest
        {
            Name = "MyLib",
            Version = "1.0.0",
            Framework = "net9.0",
            Architecture = "x64",
            Company = "Contoso"
        };

        var context = new AutoFieldsCountTestContext();
        var mdf = context.Serialize(data);

        // First 3 fields should be present
        Assert.Contains("MyLib", mdf);
        Assert.Contains("1.0.0", mdf);
        Assert.Contains("net9.0", mdf);

        // Fields beyond count should NOT be present
        Assert.DoesNotContain("x64", mdf);
        Assert.DoesNotContain("Contoso", mdf);
    }

    [Fact]
    public void Serialize_NamingPolicy_PascalCaseWords_SplitsNames()
    {
        var data = new NamingPolicyTest
        {
            AssemblyVersion = "1.0.0.0",
            PublicKeyToken = "abc123",
            TargetFramework = "net9.0"
        };

        var context = new NamingPolicyTestContext();
        var mdf = context.Serialize(data);

        // PascalCase should be split into words
        Assert.Contains("Assembly Version", mdf);
        Assert.Contains("Public Key Token", mdf);
        // Explicit [MarkoutPropertyName] should take precedence
        Assert.Contains("TFM", mdf);
        Assert.DoesNotContain("Target Framework", mdf);
    }

    [Fact]
    public void Serialize_TypeLevelSkipNull_SkipsNullProperties()
    {
        var data = new TypeLevelSkipNullTest
        {
            Name = "MyLib",
            Version = null,
            Company = "Contoso"
        };

        var context = new TypeLevelSkipNullTestContext();
        var mdf = context.Serialize(data);

        Assert.Contains("MyLib", mdf);
        Assert.Contains("Contoso", mdf);
        // Version is null and should be skipped due to type-level [MarkoutSkipNull]
        Assert.DoesNotContain("Version", mdf);
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
    public void ContextOptions_BoldFieldNames_AppliedToDefault()
    {
        var context = BoldContext.Default;
        Assert.True(context.Options.BoldFieldNames);
    }

    [Fact]
    public void ContextOptions_BoldFieldNames_RendersInOutput()
    {
        var record = new BoldRecord { Label = "Test", Value = 1 };
        var mdf = MarkoutSerializer.Serialize(record, BoldContext.Default);
        Assert.Contains("| Label |", mdf);
    }

    [Fact]
    public void Serialize_EnumProperty_RendersEnumName()
    {
        var task = new TaskItem { Name = "Build", Priority = Priority.High };
        var mdf = MarkoutSerializer.Serialize(task, EnumTestContext.Default);

        Assert.Contains("| Priority | High |", mdf);
        Assert.Contains("| Name | Build |", mdf);
    }

    [Fact]
    public void Serialize_NullableEnum_SuppressesWhenNull()
    {
        var task = new TaskItem { Name = "Build", Priority = Priority.Low, OptionalPriority = null };
        var mdf = MarkoutSerializer.Serialize(task, EnumTestContext.Default);

        Assert.Contains("| Priority | Low |", mdf);
        Assert.DoesNotContain("| OptionalPriority |", mdf);
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

        Assert.Contains("| Tags | api, web, tools |", mdf);
        Assert.Contains("| Frameworks | net8.0 &#124; net9.0 |", mdf);
        Assert.DoesNotContain("- api", mdf); // Should NOT be a bullet list
    }

    [Fact]
    public void Serialize_JoinedList_SuppressesWhenNull()
    {
        var project = new ProjectInfo { Name = "MyLib", Tags = null, Frameworks = null };
        var mdf = MarkoutSerializer.Serialize(project, JoinTestContext.Default);

        Assert.Contains("| Name | MyLib |", mdf);
        Assert.DoesNotContain("| Tags |", mdf);
        Assert.DoesNotContain("| Frameworks |", mdf);
    }

    [Fact]
    public void Serialize_JoinedList_SuppressesWhenEmpty()
    {
        var project = new ProjectInfo { Name = "MyLib", Tags = [], Frameworks = [] };
        var mdf = MarkoutSerializer.Serialize(project, JoinTestContext.Default);

        Assert.Contains("| Name | MyLib |", mdf);
        Assert.DoesNotContain("| Tags |", mdf);
        Assert.DoesNotContain("| Frameworks |", mdf);
    }

    [Fact]
    public void Serialize_FormattableProperty_UsesWriteTo()
    {
        var person = new PersonWithAddress
        {
            Name = "Alice",
            Address = new CustomAddress { Street = "123 Main St", City = "Portland", State = "OR" }
        };
        var mdf = MarkoutSerializer.Serialize(person, FormattableTestContext.Default);

        Assert.Contains("| Name | Alice |", mdf);
        Assert.Contains("Street: 123 Main St", mdf);
        Assert.Contains("City: Portland", mdf);
        Assert.Contains("State: OR", mdf);
    }

    [Fact]
    public void Serialize_FormattableProperty_SkipsWhenNull()
    {
        var person = new PersonWithAddress { Name = "Bob", Address = null };
        var mdf = MarkoutSerializer.Serialize(person, FormattableTestContext.Default);

        Assert.Contains("| Name | Bob |", mdf);
        Assert.DoesNotContain("| Street |", mdf);
        Assert.DoesNotContain("| City |", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_SuppressesFalseAndZero()
    {
        var status = new ServerStatus { Name = "Server1", IsOnline = false, ConnectionCount = 0, AlertLevel = Priority.Low, BytesTransferred = 0, CpuUsage = 0 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultTestContext.Default);

        Assert.Contains("| Name | Server1 |", mdf);
        Assert.DoesNotContain("| IsOnline |", mdf);
        Assert.DoesNotContain("| ConnectionCount |", mdf);
        Assert.DoesNotContain("| AlertLevel |", mdf);
        Assert.DoesNotContain("| BytesTransferred |", mdf);
        Assert.DoesNotContain("| CpuUsage |", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_RendersNonDefaultValues()
    {
        var status = new ServerStatus { Name = "Server1", IsOnline = true, ConnectionCount = 42, AlertLevel = Priority.High, BytesTransferred = 1024, CpuUsage = 85.5 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultTestContext.Default);

        Assert.Contains("| Name | Server1 |", mdf);
        Assert.Contains("| Is Online |", mdf);
        Assert.Contains("| Connection Count |", mdf);
        Assert.Contains("| Alert Level |", mdf);
        Assert.Contains("| Bytes Transferred |", mdf);
        Assert.Contains("| Cpu Usage |", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_LineBreaksLayout()
    {
        var status = new ServerStatusLineBreaks { Name = "Server1", IsOnline = false, ConnectionCount = 0 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultLineBreaksContext.Default);

        Assert.Contains("Name", mdf);
        Assert.DoesNotContain("Is Online", mdf);
        Assert.DoesNotContain("Connection Count", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_LineBreaksRendersNonDefault()
    {
        var status = new ServerStatusLineBreaks { Name = "Server1", IsOnline = true, ConnectionCount = 5 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultLineBreaksContext.Default);

        Assert.Contains("Is Online", mdf);
        Assert.Contains("Connection Count", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_ListLayout()
    {
        var status = new ServerStatusList { Name = "Server1", IsOnline = false, ConnectionCount = 0 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultListContext.Default);

        Assert.Contains("Name", mdf);
        Assert.DoesNotContain("Is Online", mdf);
        Assert.DoesNotContain("Connection Count", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_ListRendersNonDefault()
    {
        var status = new ServerStatusList { Name = "Server1", IsOnline = true, ConnectionCount = 10 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultListContext.Default);

        Assert.Contains("Is Online", mdf);
        Assert.Contains("Connection Count", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_PlainLayout()
    {
        var status = new ServerStatusPlain { Name = "Server1", IsOnline = false, ConnectionCount = 0 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultPlainContext.Default);

        Assert.Contains("Name", mdf);
        Assert.DoesNotContain("Is Online", mdf);
        Assert.DoesNotContain("Connection Count", mdf);
        // Plain layout: no bullets, no numbers, no table pipes
        Assert.DoesNotContain("- ", mdf);
        Assert.DoesNotContain("1.", mdf);
        Assert.DoesNotContain("| ", mdf);
    }

    [Fact]
    public void Serialize_SkipDefault_PlainRendersNonDefault()
    {
        var status = new ServerStatusPlain { Name = "Server1", IsOnline = true, ConnectionCount = 10 };
        var mdf = MarkoutSerializer.Serialize(status, SkipDefaultPlainContext.Default);

        Assert.Contains("Is Online", mdf);
        Assert.Contains("Connection Count", mdf);
        // Plain layout: no bullets, no numbers, no table pipes
        Assert.DoesNotContain("- ", mdf);
        Assert.DoesNotContain("1.", mdf);
        Assert.DoesNotContain("| ", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_SuppressesNullStrings()
    {
        var pkg = new PackageInfo { Name = "MyPackage", License = null, Repository = null, Downloads = 100 };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullTestContext.Default);

        Assert.Contains("| Name | MyPackage |", mdf);
        Assert.Contains("| Downloads | 100 |", mdf);
        Assert.DoesNotContain("| License |", mdf);
        Assert.DoesNotContain("| Repository |", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_RendersNonNullStrings()
    {
        var pkg = new PackageInfo { Name = "MyPackage", License = "MIT", Repository = "https://github.com/test", Downloads = 100 };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullTestContext.Default);

        Assert.Contains("| License | MIT |", mdf);
        Assert.Contains("| Repository | https://github.com/test |", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_SuppressesNullNullableInt()
    {
        var pkg = new PackageInfo { Name = "MyPackage", Downloads = 100, Stars = null };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullTestContext.Default);

        Assert.DoesNotContain("Stars", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_RendersNonNullNullableInt()
    {
        var pkg = new PackageInfo { Name = "MyPackage", Downloads = 100, Stars = 42 };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullTestContext.Default);

        Assert.Contains("| Stars | 42 |", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_DoesNotSuppressZeroOrFalse()
    {
        var pkg = new PackageInfo { Name = "MyPackage", Downloads = 0, Stars = 0 };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullTestContext.Default);

        Assert.Contains("| Downloads | 0 |", mdf);
        Assert.Contains("| Stars | 0 |", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_LineBreaksLayout()
    {
        var pkg = new PackageInfoLineBreaks { Name = "MyPackage", License = null, Repository = null };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullLineBreaksContext.Default);

        Assert.Contains("Name", mdf);
        Assert.DoesNotContain("License", mdf);
        Assert.DoesNotContain("Repository", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_LineBreaksRendersNonNull()
    {
        var pkg = new PackageInfoLineBreaks { Name = "MyPackage", License = "MIT", Repository = "https://example.com" };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullLineBreaksContext.Default);

        Assert.Contains("License", mdf);
        Assert.Contains("Repository", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_ListLayout()
    {
        var pkg = new PackageInfoList { Name = "MyPackage", License = null, Repository = null };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullListContext.Default);

        Assert.Contains("Name", mdf);
        Assert.DoesNotContain("License", mdf);
        Assert.DoesNotContain("Repository", mdf);
    }

    [Fact]
    public void Serialize_SkipNull_ListRendersNonNull()
    {
        var pkg = new PackageInfoList { Name = "MyPackage", License = "MIT", Repository = "https://example.com" };
        var mdf = MarkoutSerializer.Serialize(pkg, SkipNullListContext.Default);

        Assert.Contains("License", mdf);
        Assert.Contains("Repository", mdf);
    }

    [Fact]
    public void Serialize_DisplayFormat_AppliesFormatString()
    {
        var stats = new DownloadStats { Name = "MyPackage", TotalDownloads = 1234567, CpuUsage = 0.856 };
        var mdf = MarkoutSerializer.Serialize(stats, DisplayFormatTestContext.Default);

        Assert.Contains("1,234,567 downloads", mdf);
        Assert.Contains("85.6", mdf);  // P1 format: "85.6 %"
    }

    [Fact]
    public void Serialize_DisplayFormat_InTableContext()
    {
        var report = new DownloadReport
        {
            Title = "Stats",
            Packages = new List<DownloadRow>
            {
                new() { Name = "Pkg1", Downloads = 5000000 },
                new() { Name = "Pkg2", Downloads = 42 }
            }
        };
        var mdf = MarkoutSerializer.Serialize(report, DisplayFormatTestContext.Default);

        Assert.Contains("5,000,000 downloads", mdf);
        Assert.Contains("42 downloads", mdf);
    }

    [Fact]
    public void Serialize_ShowWhenProperty_ShowsSection()
    {
        var report = new ConditionalReport
        {
            Title = "Report",
            ShowDetails = true,
            ShowSummary = false,
            Details = new() { "Detail 1", "Detail 2" },
            Summary = new() { "Summary line" }
        };
        var mdf = MarkoutSerializer.Serialize(report, ShowWhenTestContext.Default);

        Assert.Contains("## Details", mdf);
        Assert.Contains("Detail 1", mdf);
        Assert.DoesNotContain("## Summary", mdf);
        Assert.DoesNotContain("Summary line", mdf);
    }

    [Fact]
    public void Serialize_ShowWhenProperty_HidesSection()
    {
        var report = new ConditionalReport
        {
            Title = "Report",
            ShowDetails = false,
            ShowSummary = true,
            Details = new() { "Detail 1" },
            Summary = new() { "Summary line" }
        };
        var mdf = MarkoutSerializer.Serialize(report, ShowWhenTestContext.Default);

        Assert.DoesNotContain("## Details", mdf);
        Assert.Contains("## Summary", mdf);
        Assert.Contains("Summary line", mdf);
    }

    [Fact]
    public void Serialize_ShowWhenProperty_MutualExclusion()
    {
        var report = new ConditionalReport
        {
            Title = "Report",
            ShowDetails = true,
            ShowSummary = true,
            Details = new() { "Detail 1" },
            Summary = new() { "Summary line" }
        };
        var mdf = MarkoutSerializer.Serialize(report, ShowWhenTestContext.Default);

        Assert.Contains("## Details", mdf);
        Assert.Contains("## Summary", mdf);
    }

    [Fact]
    public void Serialize_MaxItems_TruncatesStringArray()
    {
        var report = new FileReport
        {
            Title = "Report",
            Files = new() { "a.cs", "b.cs", "c.cs", "d.cs", "e.cs" }
        };
        var mdf = MarkoutSerializer.Serialize(report, MaxItemsTestContext.Default);

        Assert.Contains("a.cs", mdf);
        Assert.Contains("b.cs", mdf);
        Assert.Contains("c.cs", mdf);
        Assert.DoesNotContain("d.cs", mdf);
        Assert.DoesNotContain("e.cs", mdf);
        Assert.Contains("... and 2 more", mdf);
    }

    [Fact]
    public void Serialize_MaxItems_CustomEllipsisFormat()
    {
        var report = new FileReport
        {
            Title = "Report",
            Logs = new() { "log1", "log2", "log3", "log4", "log5" }
        };
        var mdf = MarkoutSerializer.Serialize(report, MaxItemsTestContext.Default);

        Assert.Contains("log1", mdf);
        Assert.Contains("log2", mdf);
        Assert.DoesNotContain("log3", mdf);
        Assert.Contains("(3 more items not shown)", mdf);
    }

    [Fact]
    public void Serialize_MaxItems_NoTruncationWhenUnderLimit()
    {
        var report = new FileReport
        {
            Title = "Report",
            Files = new() { "a.cs", "b.cs" }
        };
        var mdf = MarkoutSerializer.Serialize(report, MaxItemsTestContext.Default);

        Assert.Contains("a.cs", mdf);
        Assert.Contains("b.cs", mdf);
        Assert.DoesNotContain("... and", mdf);
    }

    [Fact]
    public void Serialize_TableDisplay_FormatsInTableContext()
    {
        var view = new Inventory
        {
            Title = "Inventory",
            Items = new()
            {
                new() { Name = "Widgets", Count = 42 },
                new() { Name = "Gadgets", Count = 7 }
            }
        };
        var mdf = MarkoutSerializer.Serialize(view, TableDisplayTestContext.Default);

        Assert.Contains("42 items", mdf);
        Assert.Contains("7 items", mdf);
    }

    [Fact]
    public void Serialize_TableDisplay_BlockContextUsesNormalFormat()
    {
        var item = new ItemRow { Name = "Widget", Count = 42 };
        var mdf = MarkoutSerializer.Serialize(item, TableDisplayTestContext.Default);

        // In block context, Count should render as "42" not "42 items"
        Assert.Contains("| Count | 42 |", mdf);
        Assert.DoesNotContain("42 items", mdf);
    }

    // --- ShowWhen tests ---

    [Fact]
    public void Serialize_ShowWhen_LineBreaks_ShowsWhenTrue()
    {
        var pkg = new VerifiedPackage { Name = "MyPkg", IsVerified = true, VerifiedBy = "Microsoft", IsTool = false, ToolFormat = "global" };
        var mdf = MarkoutSerializer.Serialize(pkg, ShowWhenTestContext.Default);

        Assert.Contains("| Verified By | Microsoft |", mdf);
        Assert.DoesNotContain("| Tool Format |", mdf);
    }

    [Fact]
    public void Serialize_ShowWhen_LineBreaks_HidesWhenFalse()
    {
        var pkg = new VerifiedPackage { Name = "MyPkg", IsVerified = false, VerifiedBy = "Microsoft", IsTool = false, ToolFormat = "global" };
        var mdf = MarkoutSerializer.Serialize(pkg, ShowWhenTestContext.Default);

        Assert.DoesNotContain("Verified By", mdf);
        Assert.DoesNotContain("Tool Format", mdf);
    }

    [Fact]
    public void Serialize_ShowWhen_OneLine_ShowsWhenTrue()
    {
        var pkg = new VerifiedPackageOneLine { Name = "MyPkg", IsVerified = true, VerifiedBy = "Microsoft" };
        var mdf = MarkoutSerializer.Serialize(pkg, ShowWhenTestContext.Default);

        Assert.Contains("| Verified By | Microsoft |", mdf);
    }

    [Fact]
    public void Serialize_ShowWhen_OneLine_HidesWhenFalse()
    {
        var pkg = new VerifiedPackageOneLine { Name = "MyPkg", IsVerified = false, VerifiedBy = "Microsoft" };
        var mdf = MarkoutSerializer.Serialize(pkg, ShowWhenTestContext.Default);

        Assert.DoesNotContain("Verified By", mdf);
    }

    [Fact]
    public void Serialize_ShowWhen_List_ShowsWhenTrue()
    {
        var pkg = new VerifiedPackageList { Name = "MyPkg", IsVerified = true, VerifiedBy = "Microsoft" };
        var mdf = MarkoutSerializer.Serialize(pkg, ShowWhenTestContext.Default);

        Assert.Contains("Verified By: Microsoft", mdf);
    }

    [Fact]
    public void Serialize_ShowWhen_List_HidesWhenFalse()
    {
        var pkg = new VerifiedPackageList { Name = "MyPkg", IsVerified = false, VerifiedBy = "Microsoft" };
        var mdf = MarkoutSerializer.Serialize(pkg, ShowWhenTestContext.Default);

        Assert.DoesNotContain("Verified By", mdf);
    }

    // --- Link tests ---

    [Fact]
    public void Serialize_Link_LineBreaks_BareLink()
    {
        var project = new LinkProjectInfo { Name = "Markout", Homepage = "https://github.com/markout" };
        var mdf = MarkoutSerializer.Serialize(project, LinkTestContext.Default);

        Assert.Contains("[https://github.com/markout](https://github.com/markout)", mdf);
    }

    [Fact]
    public void Serialize_Link_LineBreaks_WithTextProperty()
    {
        var project = new LinkProjectInfo { Name = "Markout", Repository = "https://github.com/markout" };
        var mdf = MarkoutSerializer.Serialize(project, LinkTestContext.Default);

        Assert.Contains("[Markout](https://github.com/markout)", mdf);
    }

    [Fact]
    public void Serialize_Link_LineBreaks_NullSkipped()
    {
        var project = new LinkProjectInfo { Name = "Markout" };
        var mdf = MarkoutSerializer.Serialize(project, LinkTestContext.Default);

        Assert.DoesNotContain("Homepage", mdf);
        Assert.DoesNotContain("Repository", mdf);
    }

    [Fact]
    public void Serialize_Link_OneLine_BareLink()
    {
        var project = new LinkProjectOneLine { Name = "Markout", Url = "https://example.com" };
        var mdf = MarkoutSerializer.Serialize(project, LinkTestContext.Default);

        Assert.Contains("[https://example.com](https://example.com)", mdf);
    }

    [Fact]
    public void Serialize_Link_List_BareAndTextProperty()
    {
        var project = new LinkProjectList { Name = "Markout", Url = "https://example.com", Repository = "https://github.com/markout" };
        var mdf = MarkoutSerializer.Serialize(project, LinkTestContext.Default);

        Assert.Contains("[https://example.com](https://example.com)", mdf);
        Assert.Contains("[Markout](https://github.com/markout)", mdf);
    }

    [Fact]
    public void Serialize_Link_InTable()
    {
        var doc = new ProjectListDocument
        {
            Title = "Projects",
            Projects = new List<LinkProjectRow>
            {
                new() { Name = "Alpha", Url = "https://alpha.com" },
                new() { Name = "Beta", Url = "https://beta.com" }
            }
        };
        var mdf = MarkoutSerializer.Serialize(doc, LinkTestContext.Default);

        Assert.Contains("[https://alpha.com](https://alpha.com)", mdf);
        Assert.Contains("[https://beta.com](https://beta.com)", mdf);
    }

    [Fact]
    public void Serialize_IgnoreProperty_CompactTable_OmitsDescription()
    {
        var data = new ApiWithIgnoreProperty
        {
            Title = "TestApi",
            TypesCompact = new List<TypeRow>
            {
                new() { Name = "Foo", Kind = "class", Description = "A foo type" },
                new() { Name = "Bar", Kind = "struct", Description = "A bar type" }
            }
        };

        var context = new SectionTestContext();
        var mdf = context.Serialize(data);

        Assert.Contains("## Types", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Kind |", mdf);
        Assert.DoesNotContain("| Description |", mdf);
        Assert.DoesNotContain("A foo type", mdf);
        Assert.Contains("Foo", mdf);
    }

    [Fact]
    public void Serialize_IgnoreProperty_FullTable_IncludesDescription()
    {
        var data = new ApiWithIgnoreProperty
        {
            Title = "TestApi",
            TypesFull = new List<TypeRow>
            {
                new() { Name = "Foo", Kind = "class", Description = "A foo type" },
                new() { Name = "Bar", Kind = "struct", Description = "A bar type" }
            }
        };

        var context = new SectionTestContext();
        var mdf = context.Serialize(data);

        Assert.Contains("## Types", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Kind |", mdf);
        Assert.Contains("| Description |", mdf);
        Assert.Contains("A foo type", mdf);
    }

    [Fact]
    public void Serialize_IgnoreProperty_NeitherPopulated_NoSection()
    {
        var data = new ApiWithIgnoreProperty
        {
            Title = "TestApi"
        };

        var context = new SectionTestContext();
        var mdf = context.Serialize(data);

        Assert.DoesNotContain("## Types", mdf);
    }

    [Fact]
    public void Serialize_FormattedSection_UsesFormatterAndColumnName()
    {
        var data = new ApiWithFormatter
        {
            Title = "TestApi",
            QuietMethods = new List<MethodRow>
            {
                new() { Name = "GetName", Signature = "string GetName()" },
                new() { Name = "Run", Signature = "void Run(int count)" }
            }
        };

        var context = new FormatterTestContext();
        var mdf = context.Serialize(data);

        // Column header should be overridden to "Return Type"
        Assert.Contains("| Return Type |", mdf);
        Assert.DoesNotContain("| Signature |", mdf);
        // Cell values should be formatted (first word only)
        Assert.Contains("string", mdf);
        Assert.Contains("void", mdf);
        Assert.DoesNotContain("GetName()", mdf);
    }

    [Fact]
    public void Serialize_UnformattedSection_UsesOriginalColumnAndValue()
    {
        var data = new ApiWithFormatter
        {
            Title = "TestApi",
            DetailedMethods = new List<MethodRow>
            {
                new() { Name = "GetName", Signature = "string GetName()" }
            }
        };

        var context = new FormatterTestContext();
        var mdf = context.Serialize(data);

        // Column header should be original property name
        Assert.Contains("| Signature |", mdf);
        Assert.DoesNotContain("| Return Type |", mdf);
        // Full signature should be present
        Assert.Contains("string GetName()", mdf);
    }

    [Fact]
    public void Serialize_FormattedSection_NullValue_RendersEmptyCell()
    {
        var data = new ApiWithFormatter
        {
            Title = "TestApi",
            QuietMethods = new List<MethodRow>
            {
                new() { Name = "Unknown", Signature = null }
            }
        };

        var context = new FormatterTestContext();
        var mdf = context.Serialize(data);

        // Should not throw; should render the row
        Assert.Contains("Unknown", mdf);
        Assert.Contains("| Return Type |", mdf);
    }

    // --- Scalar section tests ---

    [Fact]
    public void Serialize_ScalarSection_RendersHeadingAndFields()
    {
        var pkg = new PackageWithScalarSection
        {
            Name = "MyPackage",
            TotalDownloads = 1000,
            VersionDownloads = 200,
            VersionCount = 5
        };
        var mdf = MarkoutSerializer.Serialize(pkg, ScalarSectionTestContext.Default);

        Assert.Contains("# MyPackage", mdf);
        Assert.Contains("## Statistics", mdf);
        Assert.Contains("Total Downloads", mdf);
        Assert.Contains("1000", mdf);
        Assert.Contains("Version Downloads", mdf);
        Assert.Contains("Version Count", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_SkipNullHidesEntireSection()
    {
        var pkg = new PackageWithScalarSection
        {
            Name = "MyPackage",
            TotalDownloads = null,
            VersionDownloads = null,
            VersionCount = null
        };
        var mdf = MarkoutSerializer.Serialize(pkg, ScalarSectionTestContext.Default);

        Assert.Contains("# MyPackage", mdf);
        Assert.DoesNotContain("Statistics", mdf);
        Assert.DoesNotContain("Total Downloads", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_MultipleSectionGroups()
    {
        var view = new MultiSection
        {
            Name = "TestView",
            Author = "Alice",
            License = "MIT",
            Downloads = 100,
            Stars = 50
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.Contains("## Info", mdf);
        Assert.Contains("| Author | Alice |", mdf);
        Assert.Contains("| License | MIT |", mdf);
        Assert.Contains("## Stats", mdf);
        Assert.Contains("| Downloads | 100 |", mdf);
        Assert.Contains("| Stars | 50 |", mdf);

        // Info should come before Stats
        var infoIdx = mdf.IndexOf("## Info", StringComparison.Ordinal);
        var statsIdx = mdf.IndexOf("## Stats", StringComparison.Ordinal);
        Assert.True(infoIdx < statsIdx);
    }

    [Fact]
    public void Serialize_ScalarSection_MixedWithCollectionSection()
    {
        var view = new MixedSection
        {
            Name = "MixedView",
            Downloads = 100,
            Stars = 50,
            Dependencies = new List<SimpleDep>
            {
                new() { Id = "Dep1", Version = "1.0" }
            }
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.Contains("## Stats", mdf);
        Assert.Contains("| Downloads | 100 |", mdf);
        Assert.Contains("## Dependencies", mdf);
        Assert.Contains("Dep1", mdf);

        // Stats should come before Dependencies (declaration order)
        var statsIdx = mdf.IndexOf("## Stats", StringComparison.Ordinal);
        var depsIdx = mdf.IndexOf("## Dependencies", StringComparison.Ordinal);
        Assert.True(statsIdx < depsIdx);
    }

    [Fact]
    public void Serialize_ScalarSection_ShowWhenPropertyWrapsGroup()
    {
        var view = new ConditionalScalarSection
        {
            Name = "TestView",
            ShowStats = false,
            Downloads = 100,
            Stars = 50
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.DoesNotContain("Stats", mdf);
        Assert.DoesNotContain("Downloads", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_ShowWhenPropertyTrue()
    {
        var view = new ConditionalScalarSection
        {
            Name = "TestView",
            ShowStats = true,
            Downloads = 100,
            Stars = 50
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.Contains("## Stats", mdf);
        Assert.Contains("| Downloads | 100 |", mdf);
        Assert.Contains("| Stars | 50 |", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_FormatterAndLinkWork()
    {
        var view = new FormattedScalarSection
        {
            Name = "TestView",
            Downloads = 50,
            Repository = "https://github.com/test"
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.Contains("## Stats", mdf);
        Assert.Contains("100", mdf); // DoubleFormatter: 50 * 2
        Assert.Contains("[https://github.com/test](https://github.com/test)", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_LineBreaksLayout()
    {
        var view = new ScalarSectionLineBreaks
        {
            Name = "TestView",
            TotalDownloads = 1000,
            PackageSize = 2048
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.Contains("## Stats", mdf);
        Assert.Contains("Total Downloads", mdf);
        Assert.Contains("1000", mdf);
        Assert.Contains("Package Size", mdf);
        Assert.Contains("2048", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_LineBreaksAllNull_HidesSection()
    {
        var view = new ScalarSectionLineBreaks
        {
            Name = "TestView",
            TotalDownloads = null,
            PackageSize = null
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        Assert.DoesNotContain("Stats", mdf);
        Assert.DoesNotContain("Total Downloads", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_FieldCollectionBeforeSectionsPreservesOrder()
    {
        var view = new FieldCollectionBeforeSections
        {
            Name = "TestPkg",
            Summary = [new("Version", "1.0"), new("Type", "Library")],
            Downloads = 500,
            Stars = 10
        };
        var mdf = MarkoutSerializer.Serialize(view, ScalarSectionTestContext.Default);

        // Summary (non-section field collection) should appear before the Stats section
        var summaryIdx = mdf.IndexOf("Version: 1.0");
        var statsIdx = mdf.IndexOf("## Stats");
        Assert.True(summaryIdx >= 0, "Summary fields should be present");
        Assert.True(statsIdx >= 0, "Stats section should be present");
        Assert.True(summaryIdx < statsIdx, "Summary should appear before Stats section");
    }

    [Fact]
    public void Serialize_ScalarSection_IncludeNone_FieldCollectionStillRenders()
    {
        var view = new FieldCollectionBeforeSections
        {
            Name = "TestPkg",
            Summary = [new("Version", "1.0"), new("Type", "Library")],
            Downloads = 500,
            Stars = 10
        };
        var context = new ScalarSectionTestContext(new MarkoutWriterOptions { IncludeSections = [] });
        var mdf = context.Serialize(view);

        // Non-section field collection should render even when no sections are included
        Assert.Contains("Version: 1.0", mdf);
        Assert.Contains("Type: Library", mdf);
        // Section should be excluded
        Assert.DoesNotContain("## Stats", mdf);
        Assert.DoesNotContain("Downloads", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_IncludeOnlyDependencies_ScalarSectionExcluded()
    {
        var view = new MixedSection
        {
            Name = "TestPkg",
            Downloads = 500,
            Stars = 10,
            Dependencies = [new SimpleDep { Id = "Dep1", Version = "1.0" }]
        };
        var context = new ScalarSectionTestContext(new MarkoutWriterOptions { IncludeSections = ["Dependencies"] });
        var mdf = context.Serialize(view);

        // Scalar section should be excluded
        Assert.DoesNotContain("## Stats", mdf);
        Assert.DoesNotContain("Downloads", mdf);
        // Collection section should still render
        Assert.Contains("## Dependencies", mdf);
        Assert.Contains("Dep1", mdf);
    }

    [Fact]
    public void Serialize_ScalarSection_IncludeScalarSection_OnlyThatSectionRenders()
    {
        var view = new MixedSection
        {
            Name = "TestPkg",
            Downloads = 500,
            Stars = 10,
            Dependencies = [new SimpleDep { Id = "Dep1", Version = "1.0" }]
        };
        var context = new ScalarSectionTestContext(new MarkoutWriterOptions { IncludeSections = ["Stats"] });
        var mdf = context.Serialize(view);

        // Scalar section should render
        Assert.Contains("## Stats", mdf);
        Assert.Contains("| Downloads | 500 |", mdf);
        // Collection section should be excluded
        Assert.DoesNotContain("## Dependencies", mdf);
        Assert.DoesNotContain("Dep1", mdf);
    }

    [Fact]
    public void Serialize_PartialHooks_OnSerializingAndOnSerializedCalled()
    {
        PackageWithSectionsMarkoutTypeInfo.OnSerializingCalled = false;
        PackageWithSectionsMarkoutTypeInfo.OnSerializedCalled = false;

        var pkg = new PackageWithSections
        {
            Name = "TestPkg",
            Version = "1.0",
            Dependencies = [new SimpleDep { Id = "Dep1", Version = "2.0" }]
        };
        MarkoutSerializer.Serialize(pkg, SectionTestContext.Default);

        Assert.True(PackageWithSectionsMarkoutTypeInfo.OnSerializingCalled);
        Assert.True(PackageWithSectionsMarkoutTypeInfo.OnSerializedCalled);
    }

    [Fact]
    public void Serialize_PartialHooks_SkipSectionWhenRequested()
    {
        PackageWithSectionsMarkoutTypeInfo.SkipAssembliesSection = true;
        try
        {
            var pkg = new PackageWithSections
            {
                Name = "TestPkg",
                Version = "1.0",
                Dependencies = [new SimpleDep { Id = "Dep1", Version = "2.0" }],
                Assemblies = [new SimpleAsm { Name = "Test.dll", Arch = "x64" }]
            };
            var mdf = MarkoutSerializer.Serialize(pkg, SectionTestContext.Default);

            Assert.Contains("Dependencies", mdf);
            Assert.DoesNotContain("Assemblies", mdf);
            Assert.DoesNotContain("Test.dll", mdf);
        }
        finally
        {
            PackageWithSectionsMarkoutTypeInfo.SkipAssembliesSection = false;
        }
    }

    [Fact]
    public void Serialize_PartialHooks_SectionRendersWhenNotSkipped()
    {
        PackageWithSectionsMarkoutTypeInfo.SkipAssembliesSection = false;

        var pkg = new PackageWithSections
        {
            Name = "TestPkg",
            Version = "1.0",
            Assemblies = [new SimpleAsm { Name = "Test.dll", Arch = "x64" }]
        };
        var mdf = MarkoutSerializer.Serialize(pkg, SectionTestContext.Default);

        Assert.Contains("Assemblies", mdf);
        Assert.Contains("Test.dll", mdf);
    }

    // --- ValueMap tests ---

    [Fact]
    public void Serialize_ValueMap_PrependsBadge()
    {
        var item = new CveItem { Id = "CVE-2025-001", Severity = "CRITICAL" };
        var mdf = MarkoutSerializer.Serialize(item, ValueMapTestContext.Default);

        Assert.Contains("🔴 CRITICAL", mdf);
    }

    [Fact]
    public void Serialize_ValueMap_UnmappedValuePassesThrough()
    {
        var item = new CveItem { Id = "CVE-2025-002", Severity = "UNKNOWN" };
        var mdf = MarkoutSerializer.Serialize(item, ValueMapTestContext.Default);

        Assert.Contains("UNKNOWN", mdf);
        Assert.DoesNotContain("🔴", mdf);
        Assert.DoesNotContain("🟠", mdf);
    }

    [Fact]
    public void Serialize_ValueMap_InTableContext()
    {
        var report = new CveReport
        {
            Title = "Security Report",
            Items =
            [
                new() { Id = "CVE-2025-001", Severity = "HIGH" },
                new() { Id = "CVE-2025-002", Severity = "LOW" }
            ]
        };
        var mdf = MarkoutSerializer.Serialize(report, ValueMapTestContext.Default);

        Assert.Contains("🟠 HIGH", mdf);
        Assert.Contains("🟢 LOW", mdf);
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
[MarkoutContext(typeof(ApiWithIgnoreProperty))]
[MarkoutContext(typeof(TypeRow))]
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
[MarkoutContext(typeof(AutoFieldsWarningTest))]
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
// This verifies the analyzer correctly warns when AutoFields=false but no sections exist
#pragma warning disable MARKOUT004
[MarkoutSerializable(AutoFields = false)]
public class AutoFieldsWarningTest
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    // No [MarkoutSection] or FieldCollection properties - output will be empty
}
#pragma warning restore MARKOUT004

// Test type for AutoFieldsCount — only first N scalar properties rendered as hero fields
[MarkoutSerializable(AutoFieldsCount = 3)]
public class AutoFieldsCountTest
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Framework { get; set; } = "";
    public string Architecture { get; set; } = "";
    public string Company { get; set; } = "";
}

[MarkoutContext(typeof(AutoFieldsCountTest))]
public partial class AutoFieldsCountTestContext : MarkoutSerializerContext
{
}

// Test type for NamingPolicy — PascalCase → space-separated words (now the default)
[MarkoutSerializable]
public class NamingPolicyTest
{
    public string AssemblyVersion { get; set; } = "";
    public string PublicKeyToken { get; set; } = "";
    [MarkoutPropertyName("TFM")]
    public string TargetFramework { get; set; } = "";
}

[MarkoutContext(typeof(NamingPolicyTest))]
public partial class NamingPolicyTestContext : MarkoutSerializerContext
{
}

// Test type for type-level [MarkoutSkipNull]
[MarkoutSerializable]
[MarkoutSkipNull]
public class TypeLevelSkipNullTest
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Company { get; set; }
}

[MarkoutContext(typeof(TypeLevelSkipNullTest))]
public partial class TypeLevelSkipNullTestContext : MarkoutSerializerContext
{
}

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

// IMarkoutFormattable test types
public class CustomAddress : IMarkoutFormattable
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    public void WriteTo(MarkoutWriter writer)
    {
        writer.WriteFields(
            new MarkoutField("Street", Street ?? ""),
            new MarkoutField("City", City ?? ""),
            new MarkoutField("State", State ?? ""));
    }

    public string? ToMarkoutString() => $"{City}, {State}";
}

[MarkoutSerializable]
public class PersonWithAddress
{
    public string? Name { get; set; }
    public CustomAddress? Address { get; set; }
}

[MarkoutContext(typeof(PersonWithAddress))]
public partial class FormattableTestContext : MarkoutSerializerContext
{
}

// SkipWhenDefault test types
[MarkoutSerializable]
public class ServerStatus
{
    public string? Name { get; set; }
    [MarkoutSkipDefault]
    public bool IsOnline { get; set; }
    [MarkoutSkipDefault]
    public int ConnectionCount { get; set; }
    [MarkoutSkipDefault]
    public Priority AlertLevel { get; set; }
    [MarkoutSkipDefault]
    public long BytesTransferred { get; set; }
    [MarkoutSkipDefault]
    public double CpuUsage { get; set; }
}

[MarkoutContext(typeof(ServerStatus))]
public partial class SkipDefaultTestContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class ServerStatusLineBreaks
{
    public string? Name { get; set; }
    [MarkoutSkipDefault]
    public bool IsOnline { get; set; }
    [MarkoutSkipDefault]
    public int ConnectionCount { get; set; }
}

[MarkoutContext(typeof(ServerStatusLineBreaks))]
public partial class SkipDefaultLineBreaksContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class ServerStatusList
{
    public string? Name { get; set; }
    [MarkoutSkipDefault]
    public bool IsOnline { get; set; }
    [MarkoutSkipDefault]
    public int ConnectionCount { get; set; }
}

[MarkoutContext(typeof(ServerStatusList))]
public partial class SkipDefaultListContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(FieldLayout = FieldLayout.Plain)]
public class ServerStatusPlain
{
    public string? Name { get; set; }
    [MarkoutSkipDefault]
    public bool IsOnline { get; set; }
    [MarkoutSkipDefault]
    public int ConnectionCount { get; set; }
}

[MarkoutContext(typeof(ServerStatusPlain))]
public partial class SkipDefaultPlainContext : MarkoutSerializerContext
{
}

[MarkoutSerializable]
public class TypeRow
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Description { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class ApiWithIgnoreProperty
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Types", IgnoreProperty = nameof(TypeRow.Description))]
    public List<TypeRow>? TypesCompact { get; set; }

    [MarkoutSection(Name = "Types")]
    public List<TypeRow>? TypesFull { get; set; }
}

// Property formatter test types
[MarkoutSerializable]
public class MethodRow
{
    public string Name { get; set; } = "";
    public string? Signature { get; set; }
}

public class ReturnTypeFormatter : IMarkoutPropertyFormatter<string>
{
    public string Format(string value)
    {
        if (value == null) return "";
        var spaceIdx = value.IndexOf(' ');
        return spaceIdx > 0 ? value[..spaceIdx] : value;
    }
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class ApiWithFormatter
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Methods", FormatProperty = nameof(MethodRow.Signature),
                    Formatter = typeof(ReturnTypeFormatter), ColumnName = "Return Type")]
    public List<MethodRow>? QuietMethods { get; set; }

    [MarkoutSection(Name = "Methods")]
    public List<MethodRow>? DetailedMethods { get; set; }
}

[MarkoutContext(typeof(ApiWithFormatter))]
[MarkoutContext(typeof(MethodRow))]
public partial class FormatterTestContext : MarkoutSerializerContext
{
}

// SkipWhenNull test types
[MarkoutSerializable]
public class PackageInfo
{
    public string? Name { get; set; }
    [MarkoutSkipNull]
    public string? License { get; set; }
    [MarkoutSkipNull]
    public string? Repository { get; set; }
    public int Downloads { get; set; }
    [MarkoutSkipNull]
    public int? Stars { get; set; }
}

[MarkoutContext(typeof(PackageInfo))]
public partial class SkipNullTestContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class PackageInfoLineBreaks
{
    public string? Name { get; set; }
    [MarkoutSkipNull]
    public string? License { get; set; }
    [MarkoutSkipNull]
    public string? Repository { get; set; }
}

[MarkoutContext(typeof(PackageInfoLineBreaks))]
public partial class SkipNullLineBreaksContext : MarkoutSerializerContext
{
}

[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class PackageInfoList
{
    public string? Name { get; set; }
    [MarkoutSkipNull]
    public string? License { get; set; }
    [MarkoutSkipNull]
    public string? Repository { get; set; }
}

[MarkoutContext(typeof(PackageInfoList))]
public partial class SkipNullListContext : MarkoutSerializerContext
{
}

// DisplayFormat test types
[MarkoutSerializable]
public class DownloadStats
{
    public string? Name { get; set; }
    [MarkoutDisplayFormat("{0:N0} downloads")]
    public long TotalDownloads { get; set; }
    [MarkoutDisplayFormat("{0:P1}")]
    public double CpuUsage { get; set; }
}

[MarkoutSerializable]
public class DownloadRow
{
    public string Name { get; set; } = "";
    [MarkoutDisplayFormat("{0:N0} downloads")]
    public long Downloads { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class DownloadReport
{
    [MarkoutIgnore]
    public string? Title { get; set; }
    [MarkoutSection(Name = "Packages")]
    public List<DownloadRow>? Packages { get; set; }
}

[MarkoutContext(typeof(DownloadStats))]
[MarkoutContext(typeof(DownloadReport))]
[MarkoutContext(typeof(DownloadRow))]
public partial class DisplayFormatTestContext : MarkoutSerializerContext
{
}

// ShowWhenProperty test types
[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class ConditionalReport
{
    [MarkoutIgnore]
    public string? Title { get; set; }
    public bool ShowDetails { get; set; }
    public bool ShowSummary { get; set; }

    [MarkoutSection(Name = "Details", ShowWhenProperty = nameof(ShowDetails))]
    public List<string>? Details { get; set; }

    [MarkoutSection(Name = "Summary", ShowWhenProperty = nameof(ShowSummary))]
    public List<string>? Summary { get; set; }
}

[MarkoutContext(typeof(ConditionalReport))]
[MarkoutContext(typeof(VerifiedPackage))]
[MarkoutContext(typeof(VerifiedPackageOneLine))]
[MarkoutContext(typeof(VerifiedPackageList))]
public partial class ShowWhenTestContext : MarkoutSerializerContext
{
}

// MaxItems test types
[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class FileReport
{
    [MarkoutIgnore]
    public string? Title { get; set; }

    [MarkoutSection(Name = "Files")]
    [MarkoutMaxItems(3)]
    public List<string>? Files { get; set; }

    [MarkoutSection(Name = "Logs")]
    [MarkoutMaxItems(2, EllipsisFormat = "({0} more items not shown)")]
    public List<string>? Logs { get; set; }
}

[MarkoutContext(typeof(FileReport))]
public partial class MaxItemsTestContext : MarkoutSerializerContext
{
}

// TableDisplay test types
[MarkoutSerializable]
public class ItemRow
{
    public string Name { get; set; } = "";
    [MarkoutTableDisplay("{0} items")]
    public int Count { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class Inventory
{
    [MarkoutIgnore]
    public string? Title { get; set; }
    [MarkoutSection(Name = "Items")]
    public List<ItemRow>? Items { get; set; }
}

[MarkoutContext(typeof(Inventory))]
[MarkoutContext(typeof(ItemRow))]
public partial class TableDisplayTestContext : MarkoutSerializerContext
{
}

// --- ShowWhen test types ---

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class VerifiedPackage
{
    public string Name { get; set; } = "";
    public bool IsVerified { get; set; }
    [MarkoutShowWhen(nameof(IsVerified))]
    public string? VerifiedBy { get; set; }
    public bool IsTool { get; set; }
    [MarkoutShowWhen(nameof(IsTool))]
    public string? ToolFormat { get; set; }
}

[MarkoutSerializable]
public class VerifiedPackageOneLine
{
    public string Name { get; set; } = "";
    public bool IsVerified { get; set; }
    [MarkoutShowWhen(nameof(IsVerified))]
    public string? VerifiedBy { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class VerifiedPackageList
{
    public string Name { get; set; } = "";
    public bool IsVerified { get; set; }
    [MarkoutShowWhen(nameof(IsVerified))]
    public string? VerifiedBy { get; set; }
}

// --- Link test types ---

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class LinkProjectInfo
{
    public string Name { get; set; } = "";
    [MarkoutLink]
    public string? Homepage { get; set; }
    [MarkoutLink(TextProperty = nameof(Name))]
    public string? Repository { get; set; }
}

[MarkoutSerializable]
public class LinkProjectOneLine
{
    public string Name { get; set; } = "";
    [MarkoutLink]
    public string? Url { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class LinkProjectList
{
    public string Name { get; set; } = "";
    [MarkoutLink]
    public string? Url { get; set; }
    [MarkoutLink(TextProperty = nameof(Name))]
    public string? Repository { get; set; }
}

[MarkoutSerializable]
public class LinkProjectRow
{
    public string Name { get; set; } = "";
    [MarkoutLink]
    public string? Url { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class ProjectListDocument
{
    [MarkoutIgnore]
    public string? Title { get; set; }
    [MarkoutSection(Name = "Projects")]
    public List<LinkProjectRow>? Projects { get; set; }
}

[MarkoutContext(typeof(LinkProjectInfo))]
[MarkoutContext(typeof(LinkProjectOneLine))]
[MarkoutContext(typeof(LinkProjectList))]
[MarkoutContext(typeof(ProjectListDocument))]
[MarkoutContext(typeof(LinkProjectRow))]
public partial class LinkTestContext : MarkoutSerializerContext
{
}

// --- Scalar section test types ---

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class PackageWithScalarSection
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutSection(Name = "Statistics")]
    [MarkoutSkipNull]
    [MarkoutPropertyName("Total Downloads")]
    public long? TotalDownloads { get; set; }

    [MarkoutSection(Name = "Statistics")]
    [MarkoutSkipNull]
    [MarkoutPropertyName("Version Downloads")]
    public long? VersionDownloads { get; set; }

    [MarkoutSection(Name = "Statistics")]
    [MarkoutSkipNull]
    [MarkoutPropertyName("Version Count")]
    public int? VersionCount { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class MultiSection
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutSection(Name = "Info")]
    public string? Author { get; set; }

    [MarkoutSection(Name = "Info")]
    public string? License { get; set; }

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    public int? Downloads { get; set; }

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    public int? Stars { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class MixedSection
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    public int? Downloads { get; set; }

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    public int? Stars { get; set; }

    [MarkoutSection(Name = "Dependencies")]
    public List<SimpleDep>? Dependencies { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class ConditionalScalarSection
{
    [MarkoutIgnore] public string Name { get; set; } = "";
    public bool ShowStats { get; set; }

    [MarkoutSection(Name = "Stats", ShowWhenProperty = nameof(ShowStats))]
    public int Downloads { get; set; }

    [MarkoutSection(Name = "Stats")]
    public int Stars { get; set; }
}

public class DoubleFormatter : IMarkoutValueFormatter<long>
{
    public string Format(long value) => $"{value * 2}";
}

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class FormattedScalarSection
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutSection(Name = "Stats")]
    [MarkoutValueFormatter(typeof(DoubleFormatter))]
    public long Downloads { get; set; }

    [MarkoutSection(Name = "Stats")]
    [MarkoutLink]
    public string? Repository { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false, FieldLayout = FieldLayout.Table)]
public class ScalarSectionLineBreaks
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    [MarkoutPropertyName("Total Downloads")]
    public long? TotalDownloads { get; set; }

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    [MarkoutPropertyName("Package Size")]
    public long? PackageSize { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class FieldCollectionBeforeSections
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutIgnoreInTable]
    public List<MarkoutField> Summary { get; set; } = [];

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    public int? Downloads { get; set; }

    [MarkoutSection(Name = "Stats")]
    [MarkoutSkipNull]
    public int? Stars { get; set; }
}

[MarkoutContext(typeof(PackageWithScalarSection))]
[MarkoutContext(typeof(MultiSection))]
[MarkoutContext(typeof(MixedSection))]
[MarkoutContext(typeof(ConditionalScalarSection))]
[MarkoutContext(typeof(FormattedScalarSection))]
[MarkoutContext(typeof(ScalarSectionLineBreaks))]
[MarkoutContext(typeof(FieldCollectionBeforeSections))]
public partial class ScalarSectionTestContext : MarkoutSerializerContext
{
}

// --- ValueMap test types ---

[MarkoutSerializable]
public class CveItem
{
    public string Id { get; set; } = "";

    [MarkoutValueMap("CRITICAL=🔴", "HIGH=🟠", "MEDIUM=🟡", "LOW=🟢")]
    public string Severity { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class CveReport
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Vulnerabilities")]
    public List<CveItem>? Items { get; set; }
}

[MarkoutContext(typeof(CveItem))]
[MarkoutContext(typeof(CveReport))]
public partial class ValueMapTestContext : MarkoutSerializerContext
{
}
