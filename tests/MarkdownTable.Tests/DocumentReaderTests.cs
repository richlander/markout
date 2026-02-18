using MarkdownTable.Formatting;
using System.Text;

namespace MarkdownTable.Tests;

public class DocumentReaderTests
{
    // --- Simple table (DefaultTable) ---

    [Fact]
    public void Read_SimpleTable_PopulatesDefaultTable()
    {
        var md = """
            | Name  | Age |
            | ----- | --- |
            | Alice | 30  |
            | Bob   | 25  |
            """;

        var doc = DocumentReader.Read(md);

        Assert.NotNull(doc.DefaultTable);
        Assert.Equal(["Name", "Age"], doc.DefaultTable.Headers);
        Assert.Equal(2, doc.DefaultTable.Rows.Count);
        Assert.Equal("Alice", doc.DefaultTable.Rows[0][0]);
        Assert.Equal("30", doc.DefaultTable.Rows[0][1]);
        Assert.Equal("Bob", doc.DefaultTable.Rows[1][0]);
        Assert.Equal("25", doc.DefaultTable.Rows[1][1]);
    }

    // --- Table in a section ---

    [Fact]
    public void Read_TableUnderHeading_PopulatesSectionTable()
    {
        var md = """
            ## People

            | Name  | Age |
            | ----- | --- |
            | Alice | 30  |
            """;

        var doc = DocumentReader.Read(md);

        var section = Assert.Single(doc.Sections);
        Assert.Equal("People", section.Heading);
        Assert.Equal(2, section.Level);
        Assert.NotNull(section.Table);
        Assert.Equal(["Name", "Age"], section.Table.Headers);
        Assert.Single(section.Table.Rows);
    }

    // --- Multiple sections with tables ---

    [Fact]
    public void Read_MultipleSectionsWithTables_EachSectionHasOwnTable()
    {
        var md = """
            ## Fruits

            | Fruit  | Color  |
            | ------ | ------ |
            | Apple  | Red    |

            ## Veggies

            | Veggie  | Color  |
            | ------- | ------ |
            | Carrot  | Orange |
            """;

        var doc = DocumentReader.Read(md);

        Assert.Equal(2, doc.Sections.Count);

        Assert.Equal("Fruits", doc.Sections[0].Heading);
        var fruitsTable = doc.Sections[0].Table;
        Assert.NotNull(fruitsTable);
        Assert.Equal("Apple", fruitsTable.Rows[0][0]);

        Assert.Equal("Veggies", doc.Sections[1].Heading);
        var veggiesTable = doc.Sections[1].Table;
        Assert.NotNull(veggiesTable);
        Assert.Equal("Carrot", veggiesTable.Rows[0][0]);
    }

    // --- Fields before any heading ---

    [Fact]
    public void Read_FieldsBeforeHeading_PopulatesDocFields()
    {
        var md = """
            Name: Alice
            Age: 30
            """;

        var doc = DocumentReader.Read(md);

        Assert.True(doc.Fields.ContainsKey("Name"));
        Assert.Equal("Alice", doc.Fields["Name"].Text);
        Assert.True(doc.Fields.ContainsKey("Age"));
        Assert.Equal("30", doc.Fields["Age"].Text);
    }

    // --- Fields in a section ---

    [Fact]
    public void Read_FieldsInSection_PopulatesSectionFields()
    {
        var md = """
            ## Info

            Status: Active
            Priority: High
            """;

        var doc = DocumentReader.Read(md);

        var section = Assert.Single(doc.Sections);
        Assert.Equal("Info", section.Heading);
        Assert.True(section.Fields.ContainsKey("Status"));
        Assert.Equal("Active", section.Fields["Status"].Text);
        Assert.True(section.Fields.ContainsKey("Priority"));
        Assert.Equal("High", section.Fields["Priority"].Text);
    }

    // --- Title extraction ---

    [Fact]
    public void Read_H1Heading_SetsTitle()
    {
        var md = """
            # My Document

            Some content
            """;

        var doc = DocumentReader.Read(md);

        Assert.Equal("My Document", doc.Title);
    }

    [Fact]
    public void Read_NoH1Heading_TitleIsNull()
    {
        var md = """
            ## Section Only

            Some content
            """;

        var doc = DocumentReader.Read(md);

        Assert.Null(doc.Title);
    }

    [Fact]
    public void Read_MultipleH1_TitleIsFirst()
    {
        var md = """
            # First Title
            # Second Title
            """;

        var doc = DocumentReader.Read(md);

        Assert.Equal("First Title", doc.Title);
    }

    // --- Mixed content ---

    [Fact]
    public void Read_MixedContent_TitleFieldsAndSectionTable()
    {
        var md = """
            # Report

            Author: Jane
            Date: 2024-01-15

            ## Results

            | Metric | Value |
            | ------ | ----- |
            | Score  | 95    |
            """;

        var doc = DocumentReader.Read(md);

        Assert.Equal("Report", doc.Title);
        Assert.True(doc.Fields.ContainsKey("Author"));
        Assert.Equal("Jane", doc.Fields["Author"].Text);
        Assert.True(doc.Fields.ContainsKey("Date"));

        var resultsSection = doc.Sections.First(s => s.Heading == "Results");
        Assert.NotNull(resultsSection.Table);
        Assert.Equal("Score", resultsSection.Table.Rows[0][0]);
        Assert.Equal("95", resultsSection.Table.Rows[0][1]);
    }

    // --- Empty input ---

    [Fact]
    public void Read_EmptyString_ReturnsEmptyDocument()
    {
        var doc = DocumentReader.Read("");

        Assert.Null(doc.Title);
        Assert.Empty(doc.Sections);
        Assert.Null(doc.DefaultTable);
    }

    // --- Ragged rows ---

    [Fact]
    public void Read_TableWithVaryingColumnCounts_ParsesAvailableCells()
    {
        var md = """
            | A   | B   | C   |
            | --- | --- | --- |
            | 1   | 2   | 3   |
            | 4   | 5   |
            """;

        var doc = DocumentReader.Read(md);

        Assert.NotNull(doc.DefaultTable);
        Assert.Equal(3, doc.DefaultTable.Headers.Length);
        Assert.Equal(2, doc.DefaultTable.Rows.Count);
        Assert.Equal(3, doc.DefaultTable.Rows[0].Length);
        // Ragged row may have fewer columns
        Assert.True(doc.DefaultTable.Rows[1].Length <= 3);
    }

    // --- Read(string) and Read(byte[]) equivalence ---

    [Fact]
    public void Read_Utf8Bytes_ProducesEquivalentResult()
    {
        var md = """
            # Title

            ## Data

            | X   | Y   |
            | --- | --- |
            | 1   | 2   |
            """;

        var fromString = DocumentReader.Read(md);
        var fromBytes = DocumentReader.Read(Encoding.UTF8.GetBytes(md));

        Assert.Equal(fromString.Title, fromBytes.Title);
        Assert.Equal(fromString.Sections.Count, fromBytes.Sections.Count);

        for (int i = 0; i < fromString.Sections.Count; i++)
        {
            Assert.Equal(fromString.Sections[i].Heading, fromBytes.Sections[i].Heading);
            Assert.Equal(fromString.Sections[i].Level, fromBytes.Sections[i].Level);

            if (fromString.Sections[i].Table is { } stringTable)
            {
                var bytesTable = fromBytes.Sections[i].Table;
                Assert.NotNull(bytesTable);
                Assert.Equal(stringTable.Headers, bytesTable.Headers);
                Assert.Equal(stringTable.Rows.Count, bytesTable.Rows.Count);
            }
        }
    }

    // --- ReadAsync(Stream) equivalence ---

    [Fact]
    public async Task ReadAsync_Stream_ProducesEquivalentResult()
    {
        var md = """
            # Title

            Key: Value

            ## Section

            | Col1 | Col2 |
            | ---- | ---- |
            | A    | B    |
            """;

        var fromString = DocumentReader.Read(md);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(md));
        var fromStream = await DocumentReader.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(fromString.Title, fromStream.Title);
        Assert.Equal(fromString.Sections.Count, fromStream.Sections.Count);

        for (int i = 0; i < fromString.Sections.Count; i++)
        {
            Assert.Equal(fromString.Sections[i].Heading, fromStream.Sections[i].Heading);
            Assert.Equal(fromString.Sections[i].Level, fromStream.Sections[i].Level);

            if (fromString.Sections[i].Table is { } strTable)
            {
                var streamTable = fromStream.Sections[i].Table;
                Assert.NotNull(streamTable);
                Assert.Equal(strTable.Headers, streamTable.Headers);
            }
        }
    }

    // --- Bold fields ---

    [Fact]
    public void Read_BoldFields_ParsedAsFields()
    {
        var md = """
            **Name:** Alice
            **Role:** Engineer
            """;

        var doc = DocumentReader.Read(md);

        Assert.True(doc.Fields.ContainsKey("Name"));
        Assert.Equal("Alice", doc.Fields["Name"].Text);
        Assert.True(doc.Fields.ContainsKey("Role"));
        Assert.Equal("Engineer", doc.Fields["Role"].Text);
    }

    // --- Table separator validation ---

    [Fact]
    public void Read_MissingSeparatorLine_DoesNotParseAsTable()
    {
        var md = """
            | Name  | Age |
            | Alice | 30  |
            | Bob   | 25  |
            """;

        var doc = DocumentReader.Read(md);

        // Without a valid separator line, this should not parse as a table
        // The second line "| Alice | 30 |" is not a valid separator
        Assert.Null(doc.DefaultTable);
    }

    [Fact]
    public void Read_InvalidSeparatorCharacters_DoesNotParseAsTable()
    {
        var md = """
            | Name  | Age |
            | _____ | ___ |
            | Alice | 30  |
            """;

        var doc = DocumentReader.Read(md);

        Assert.Null(doc.DefaultTable);
    }

    [Fact]
    public void Read_ValidSeparatorWithAlignment_ParsesTable()
    {
        var md = """
            | Left | Center | Right |
            | :--- | :----: | ----: |
            | a    | b      | c     |
            """;

        var doc = DocumentReader.Read(md);

        Assert.NotNull(doc.DefaultTable);
        Assert.Equal(["Left", "Center", "Right"], doc.DefaultTable.Headers);
        Assert.Single(doc.DefaultTable.Rows);
    }

    // --- Heading levels ---

    [Theory]
    [InlineData("## H2", 2, "H2")]
    [InlineData("### H3", 3, "H3")]
    [InlineData("#### H4", 4, "H4")]
    [InlineData("##### H5", 5, "H5")]
    [InlineData("###### H6", 6, "H6")]
    public void Read_VariousHeadingLevels_ParsedCorrectly(string heading, int expectedLevel, string expectedText)
    {
        var doc = DocumentReader.Read(heading);

        var section = Assert.Single(doc.Sections);
        Assert.Equal(expectedLevel, section.Level);
        Assert.Equal(expectedText, section.Heading);
    }

    // --- DefaultTable is first table found ---

    [Fact]
    public void DefaultTable_ReturnsFirstTableInDocument()
    {
        var md = """
            ## First

            | A   |
            | --- |
            | 1   |

            ## Second

            | B   |
            | --- |
            | 2   |
            """;

        var doc = DocumentReader.Read(md);

        Assert.NotNull(doc.DefaultTable);
        Assert.Equal(["A"], doc.DefaultTable.Headers);
    }

    // --- Whitespace-only input ---

    [Fact]
    public void Read_WhitespaceOnly_ReturnsEmptyDocument()
    {
        var doc = DocumentReader.Read("   \n  \n   ");

        Assert.Null(doc.Title);
        Assert.Null(doc.DefaultTable);
    }
}
