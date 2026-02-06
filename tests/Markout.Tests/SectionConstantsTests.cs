using Markout;
using Xunit;

namespace Markout.Tests;

// --- Test model with sections ---

[MarkoutSerializable]
public class SectionConstantsModel
{
    public string? Name { get; set; }

    [MarkoutSection(Name = "Metadata")]
    public List<MarkoutField>? Metadata { get; set; }

    [MarkoutSection(Name = "Package Dependencies")]
    public List<string>? PackageDependencies { get; set; }

    [MarkoutSection(Name = "RID Packages")]
    public List<string>? RidPackages { get; set; }
}

// --- Context ---

[MarkoutContext(typeof(SectionConstantsModel))]
public partial class SectionConstantsTestContext : MarkoutSerializerContext
{
}

public class SectionConstantsTests
{
    [Fact]
    public void SectionConstants_SimpleNameExists()
    {
        Assert.Equal("Metadata", SectionConstantsTestContext.SectionConstantsModelSections.Metadata);
    }

    [Fact]
    public void SectionConstants_SpacedNameNormalized()
    {
        Assert.Equal("Package Dependencies", SectionConstantsTestContext.SectionConstantsModelSections.PackageDependencies);
    }

    [Fact]
    public void SectionConstants_AcronymPreserved()
    {
        Assert.Equal("RID Packages", SectionConstantsTestContext.SectionConstantsModelSections.RIDPackages);
    }

    [Fact]
    public void SectionConstants_UsableInIncludeSections()
    {
        var record = new SectionConstantsModel
        {
            Name = "Test",
            Metadata = new List<MarkoutField> { new("Key", "Value") },
            PackageDependencies = new List<string> { "Dep1" },
            RidPackages = new List<string> { "RID1" }
        };

        var options = new MarkoutWriterOptions
        {
            IncludeSections = new HashSet<string>
            {
                SectionConstantsTestContext.SectionConstantsModelSections.Metadata
            }
        };

        var mdf = MarkoutSerializer.Serialize(record, SectionConstantsTestContext.Default, options);

        Assert.Contains("Metadata", mdf);
        Assert.DoesNotContain("Package Dependencies", mdf);
        Assert.DoesNotContain("RID Packages", mdf);
    }

    [Fact]
    public void SectionConstants_ConstValuesMatchViaReflection()
    {
        var sectionsType = typeof(SectionConstantsTestContext).GetNestedType("SectionConstantsModelSections");
        Assert.NotNull(sectionsType);

        var fields = sectionsType!.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.Equal(3, fields.Length);

        foreach (var field in fields)
        {
            Assert.True(field.IsLiteral, $"{field.Name} should be a const");
            Assert.IsType<string>(field.GetRawConstantValue());
        }
    }
}
