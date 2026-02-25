using Markout;

namespace Markout.Tests;

public class ProjectionTests
{
    // --- Column projection: IncludeColumns ---

    [Fact]
    public void IncludeColumns_FiltersTableToSpecifiedColumns()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name", "TFM"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version", "TFM", "Signed");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0", "yes");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("TFM", output);
        Assert.Contains("Foo.dll", output);
        Assert.Contains("net8.0", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("1.0.0", output);
        Assert.DoesNotContain("Signed", output);
        Assert.DoesNotContain("yes", output);
    }

    [Fact]
    public void IncludeColumns_ReordersColumnsToMatchSpecifiedOrder()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["TFM", "Name"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        // TFM should appear before Name
        int tfmIndex = output.IndexOf("TFM");
        int nameIndex = output.IndexOf("Name");
        Assert.True(tfmIndex < nameIndex, "TFM should appear before Name in output");
    }

    [Fact]
    public void IncludeColumns_CaseInsensitiveByDefault()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["name", "tfm"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("TFM", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeColumns_UnmatchedColumnsIgnored()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name", "NonExistent"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version");
        writer.WriteTableRow("Foo.dll", "1.0.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo.dll", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Column projection: ExcludeColumns ---

    [Fact]
    public void ExcludeColumns_RemovesSpecifiedColumns()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeColumns = ["Version", "Signed"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version", "TFM", "Signed");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0", "yes");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("TFM", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("Signed", output);
    }

    [Fact]
    public void ExcludeColumns_PreservesOriginalOrder()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeColumns = ["Version"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        int nameIndex = output.IndexOf("Name");
        int tfmIndex = output.IndexOf("TFM");
        Assert.True(nameIndex < tfmIndex, "Name should appear before TFM (original order)");
    }

    // --- Column projection with WriteTable (batch API) ---

    [Fact]
    public void IncludeColumns_WorksWithBatchWriteTable()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTable(
            ["Name", "Version", "TFM"],
            [["Foo.dll", "1.0.0", "net8.0"], ["Bar.dll", "2.0.0", "net9.0"]]);
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo.dll", output);
        Assert.Contains("Bar.dll", output);
        Assert.DoesNotContain("Version", output);
        Assert.DoesNotContain("TFM", output);
    }

    // --- Column projection with MarkdownWriter ---

    [Fact]
    public void IncludeColumns_WorksWithMarkdownWriter()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name", "TFM"]
            }
        };
        var writer = new MarkdownWriter(options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("| Name", output);
        Assert.Contains("TFM", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Column projection with OneLineWriter ---

    [Fact]
    public void IncludeColumns_WorksWithOneLineWriter()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var writer = new OneLineWriter(sw, options);
        writer.WriteTableStart("Name", "Version", "TFM");
        writer.WriteTableRow("Foo.dll", "1.0.0", "net8.0");
        writer.WriteTableEnd();
        var output = sw.ToString();
        Assert.Contains("NAME", output);
        Assert.Contains("Foo.dll", output);
        Assert.DoesNotContain("VERSION", output);
        Assert.DoesNotContain("TFM", output);
    }

    // --- Field projection: IncludeFields ---

    [Fact]
    public void IncludeFields_FiltersScalarFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Name", "License"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldBareList(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "9.0.0"),
            new MarkoutField("License", "MIT"));
        var output = writer.ToString();
        Assert.Contains("Name: System.Text.Json", output);
        Assert.Contains("License: MIT", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeFields_FiltersBooleanFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Signed"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldBareList(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Signed", "yes"));
        var output = writer.ToString();
        Assert.Contains("Signed: yes", output);
        Assert.DoesNotContain("Name", output);
    }

    [Fact]
    public void IncludeFields_FiltersGenericFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Count"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldBareList(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Count", "42"));
        var output = writer.ToString();
        Assert.Contains("Count: 42", output);
        Assert.DoesNotContain("Name", output);
    }

    [Fact]
    public void IncludeFields_CaseInsensitiveByDefault()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["name"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldBareList(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"));
        var output = writer.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Field projection: ExcludeFields ---

    [Fact]
    public void ExcludeFields_RemovesSpecifiedFields()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeFields = ["Version"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldBareList(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("License", "MIT"));
        var output = writer.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("License: MIT", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Field projection: FieldList ---

    [Fact]
    public void IncludeFields_FiltersFieldList()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["Name", "TFM"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldLine(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("TFM", "net8.0"));
        var output = writer.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("TFM: net8.0", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeFields_ReordersFieldList()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["TFM", "Name"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldLine(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("TFM", "net8.0"));
        var output = writer.ToString();
        int tfmIndex = output.IndexOf("TFM");
        int nameIndex = output.IndexOf("Name");
        Assert.True(tfmIndex < nameIndex, "TFM should appear before Name in field list");
    }

    [Fact]
    public void ExcludeFields_FiltersFieldList()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                ExcludeFields = ["Version"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldLine(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"),
            new MarkoutField("TFM", "net8.0"));
        var output = writer.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("TFM: net8.0", output);
        Assert.DoesNotContain("Version", output);
    }

    [Fact]
    public void IncludeFields_AllFilteredProducesNoOutput()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection
            {
                IncludeFields = ["NonExistent"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteFieldBareList([new("Name", "Foo")]);
        writer.WriteFieldLine(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"));
        var output = writer.ToString();
        Assert.Equal("", output);
    }

    // --- Projection composes with section filtering ---

    [Fact]
    public void Projection_ComposesWithSectionFiltering()
    {
        var options = new MarkoutWriterOptions
        {
            IncludeSections = ["Details"],
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteHeading(1, "Report");
        writer.WriteFieldBareList([new("TopLevel", "value")]);
        writer.WriteHeading(2, "Details");
        writer.WriteTableStart("Name", "Version");
        writer.WriteTableRow("Foo", "1.0.0");
        writer.WriteTableEnd();
        writer.WriteHeading(2, "Other");
        writer.WriteTableStart("Name", "Version");
        writer.WriteTableRow("Bar", "2.0.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        // Details section included, Other excluded
        Assert.Contains("Foo", output);
        Assert.DoesNotContain("Bar", output);
        // Column projection applied within Details
        Assert.DoesNotContain("1.0.0", output);
    }

    // --- Mutual exclusion validation ---

    [Fact]
    public void IncludeAndExcludeColumns_ThrowsWhenBothSet()
    {
        var projection = new MarkoutProjection
        {
            IncludeColumns = ["Name"]
        };
        Assert.Throws<InvalidOperationException>(() =>
            projection.ExcludeColumns = ["Version"]);
    }

    [Fact]
    public void IncludeAndExcludeFields_ThrowsWhenBothSet()
    {
        var projection = new MarkoutProjection
        {
            IncludeFields = ["Name"]
        };
        Assert.Throws<InvalidOperationException>(() =>
            projection.ExcludeFields = ["Version"]);
    }

    // --- No projection = passthrough ---

    [Fact]
    public void NoProjection_TablePassesThrough()
    {
        var writer = new MarkoutWriter();
        writer.WriteTableStart("Name", "Version");
        writer.WriteTableRow("Foo", "1.0.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Version", output);
        Assert.Contains("Foo", output);
        Assert.Contains("1.0.0", output);
    }

    [Fact]
    public void NoProjection_FieldsPassThrough()
    {
        var writer = new MarkoutWriter();
        writer.WriteFieldBareList(
            new MarkoutField("Name", "Foo"),
            new MarkoutField("Version", "1.0.0"));
        var output = writer.ToString();
        Assert.Contains("Name: Foo", output);
        Assert.Contains("Version: 1.0.0", output);
    }

    // --- Column projection with MaxItems ---

    [Fact]
    public void IncludeColumns_ComposesWithMaxItems()
    {
        var options = new MarkoutWriterOptions
        {
            MaxItems = 1,
            Projection = new MarkoutProjection
            {
                IncludeColumns = ["Name"]
            }
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version");
        writer.WriteTableRow("Foo", "1.0.0");
        writer.WriteTableRow("Bar", "2.0.0");
        writer.WriteTableRow("Baz", "3.0.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Foo", output);
        Assert.DoesNotContain("Bar", output);
        Assert.Contains("... and 2 more", output);
        Assert.DoesNotContain("Version", output);
    }

    // --- Schema discovery ---

    [Fact]
    public void GetFieldNames_ReturnsDocumentLevelFields()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var fields = schema.GetFieldNames();
        Assert.Equal(["Name", "Version"], fields);
    }

    [Fact]
    public void GetColumnNames_ReturnsUniqueColumnsAcrossTables()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var columns = schema.GetColumnNames();
        // Dependencies has Id, Version; Assemblies has Name, Arch
        Assert.Equal(["Id", "Version", "Name", "Arch"], columns);
    }

    [Fact]
    public void GetSectionNames_ReturnsSectionHeadingNames()
    {
        var schema = SectionTestContext.PackageWithSectionsSchema;
        var sections = schema.GetSectionNames();
        Assert.Equal(["Dependencies", "Assemblies"], sections);
    }

    [Fact]
    public void ExtractSectionName_ParsesSectionRendering()
    {
        Assert.Equal("Dependencies", MarkoutSchemaInfo.ExtractSectionName("H2 Section \"Dependencies\" (table)"));
        Assert.Equal("API Surface", MarkoutSchemaInfo.ExtractSectionName("H2 Section \"API Surface\" (subsections)"));
        Assert.Null(MarkoutSchemaInfo.ExtractSectionName("Field"));
        Assert.Null(MarkoutSchemaInfo.ExtractSectionName("Table"));
    }

    [Fact]
    public void GetFieldNames_EmptyForTableOnlySchema()
    {
        // A type with no scalar fields should return empty
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "NoFields",
            AsDocument = [
                new() { Name = "Items", DisplayName = "Items", Rendering = "H2 Section \"Items\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                ] },
            ],
        };
        Assert.Empty(schema.GetFieldNames());
    }

    [Fact]
    public void GetColumnNames_EmptyForFieldOnlySchema()
    {
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "FieldsOnly",
            AsDocument = [
                new() { Name = "Name", DisplayName = "Name", Rendering = "Field" },
                new() { Name = "Version", DisplayName = "Version", Rendering = "Field" },
            ],
        };
        Assert.Empty(schema.GetColumnNames());
    }

    [Fact]
    public void GetColumnNames_DeduplicatesAcrossTables()
    {
        // Two tables both have a "Name" column — should appear once
        var schema = new MarkoutSchemaInfo
        {
            TypeName = "DupCols",
            AsDocument = [
                new() { Name = "Deps", DisplayName = "Deps", Rendering = "H2 Section \"Deps\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                    new() { Name = "Version", DisplayName = "Version", Rendering = "Column" },
                ] },
                new() { Name = "Refs", DisplayName = "Refs", Rendering = "H2 Section \"Refs\" (table)", Children = [
                    new() { Name = "Name", DisplayName = "Name", Rendering = "Column" },
                    new() { Name = "Target", DisplayName = "Target", Rendering = "Column" },
                ] },
            ],
        };
        var columns = schema.GetColumnNames();
        Assert.Equal(["Name", "Version", "Target"], columns);
    }

    // --- Factory method tests ---

    [Fact]
    public void WithColumns_CreatesProjectionWithIncludeColumns()
    {
        var projection = MarkoutProjection.WithColumns("Name", "TFM");
        Assert.Equal(["Name", "TFM"], projection.IncludeColumns);
        Assert.Null(projection.ExcludeColumns);
    }

    [Fact]
    public void WithoutColumns_CreatesProjectionWithExcludeColumns()
    {
        var projection = MarkoutProjection.WithoutColumns("Version", "Signed");
        Assert.Null(projection.IncludeColumns);
        Assert.Contains("Version", projection.ExcludeColumns!);
        Assert.Contains("Signed", projection.ExcludeColumns!);
    }

    [Fact]
    public void WithFields_CreatesProjectionWithIncludeFields()
    {
        var projection = MarkoutProjection.WithFields("Name", "License");
        Assert.Equal(["Name", "License"], projection.IncludeFields);
        Assert.Null(projection.ExcludeFields);
    }

    [Fact]
    public void WithoutFields_CreatesProjectionWithExcludeFields()
    {
        var projection = MarkoutProjection.WithoutFields("InternalId");
        Assert.Null(projection.IncludeFields);
        Assert.Contains("InternalId", projection.ExcludeFields!);
    }

    [Fact]
    public void WithSections_CreatesProjectionWithIncludeSections()
    {
        var projection = MarkoutProjection.WithSections("Details", "API Surface");
        Assert.Equal(["Details", "API Surface"], projection.IncludeSections);
    }

    [Fact]
    public void FactoryMethod_WorksWithWriterOptions()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = MarkoutProjection.WithColumns("Name")
        };
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name", "Version");
        writer.WriteTableRow("Foo", "1.0.0");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Foo", output);
        Assert.DoesNotContain("Version", output);
    }
}
