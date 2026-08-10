using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Markout.SourceGeneration;

namespace Markout.Tests;

public class SourceGeneratorDiagnosticTests
{
    [Theory]
    [InlineData("")]
    [InlineData("[MarkoutIgnore] public string Value { get; set; } = \"\";")]
    [InlineData("[MarkoutChild] public bool IsChild { get; set; }")]
    public void Markout006_ReportsRowTypesWithoutVisibleColumns(string rowMembers)
    {
        var source = $$"""
            using System.Collections.Generic;
            using Markout;

            [MarkoutSerializable]
            public class Row
            {
                {{rowMembers}}
            }

            [MarkoutSerializable]
            public class Report
            {
                public List<Row> Rows { get; set; } = new();
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        var diagnostic = Assert.Single(RunGenerator(source), d => d.Id == "MARKOUT006");
        Assert.Contains("Rows", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Markout006_ReportsRowsWhoseOnlyColumnIsSectionIgnored()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            [MarkoutSerializable]
            public class Row
            {
                public string Value { get; set; } = "";
            }

            [MarkoutSerializable]
            public class Report
            {
                [MarkoutSection(IgnoreProperty = nameof(Row.Value))]
                public List<Row> Rows { get; set; } = new();
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        Assert.Single(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_DoesNotReportRuntimeConditionalColumns()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            [MarkoutSerializable]
            public class Row
            {
                public string Value { get; set; } = "";
            }

            [MarkoutSerializable]
            public class Report
            {
                [MarkoutIgnoreColumnWhen(nameof(HideValue), nameof(Row.Value))]
                public List<Row> Rows { get; set; } = new();

                public static bool HideValue(List<Row> rows) => true;
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        Assert.DoesNotContain(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_DoesNotReportIgnoredTypeGraphs()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            public class EmptyRow;

            public class Details
            {
                public List<EmptyRow> Rows { get; set; } = new();
            }

            [MarkoutSerializable]
            public class Report
            {
                [MarkoutIgnore]
                public List<EmptyRow> DirectRows { get; set; } = new();

                [MarkoutIgnore]
                public Details HiddenDetails { get; set; } = new();
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        Assert.DoesNotContain(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_DoesNotTreatCycleTruncationAsAnEmptyRowType()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            [MarkoutSerializable]
            public class Node
            {
                public string Name { get; set; } = "";
                public List<Node> Children { get; set; } = new();
            }

            [MarkoutContext(typeof(Node))]
            public partial class NodeContext : MarkoutSerializerContext { }
            """;

        Assert.DoesNotContain(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_DoesNotReportScalarCollections()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            public enum State { Ready }

            [MarkoutSerializable]
            public class Report
            {
                public List<int> Counts { get; set; } = new();
                public int[] Values { get; set; } = [];
                public List<State> States { get; set; } = new();
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        Assert.DoesNotContain(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_DoesNotTreatMutualRecursionAsAnEmptyRowType()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            [MarkoutSerializable]
            public class Parent
            {
                public string Name { get; set; } = "";
                public List<Child> Children { get; set; } = new();
            }

            public class Child
            {
                public string Name { get; set; } = "";
                public List<Parent> Parents { get; set; } = new();
            }

            [MarkoutContext(typeof(Parent))]
            public partial class ParentContext : MarkoutSerializerContext { }
            """;

        Assert.DoesNotContain(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_DoesNotReportCollectionsExcludedByAutoFields()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            public class EmptyRow;

            public class VisibleRow
            {
                public string Value { get; set; } = "";
            }

            [MarkoutSerializable(AutoFields = false)]
            public class Report
            {
                [MarkoutSection]
                public List<VisibleRow> Visible { get; set; } = new();

                [MarkoutIgnoreInTable]
                public List<EmptyRow> Cache { get; set; } = new();
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        Assert.DoesNotContain(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    [Fact]
    public void Markout006_ReportsEmptySectionRowsWhenAutoFieldsAreDisabled()
    {
        const string source = """
            using System.Collections.Generic;
            using Markout;

            public class EmptyRow;

            [MarkoutSerializable(AutoFields = false)]
            public class Report
            {
                [MarkoutSection]
                public List<EmptyRow> Rows { get; set; } = new();
            }

            [MarkoutContext(typeof(Report))]
            public partial class ReportContext : MarkoutSerializerContext { }
            """;

        Assert.Single(RunGenerator(source), d => d.Id == "MARKOUT006");
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            $"GeneratorTest_{Guid.NewGuid():N}",
            [syntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new MarkoutSourceGenerator());
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Diagnostics;
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.NotNull(trustedAssemblies);

        foreach (var path in trustedAssemblies!.Split(Path.PathSeparator))
            yield return MetadataReference.CreateFromFile(path);

        yield return MetadataReference.CreateFromFile(
            Path.Combine(AppContext.BaseDirectory, "Markout.dll"));
    }
}
