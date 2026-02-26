using Markout;

namespace Markout.Tests;

#region Nested Structure Test Models

// Simple nested object (1 level deep)
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }

        [MarkoutSection(Name = "Contact Info")]
        public ContactInfo? Contact { get; set; }
    }

    [MarkoutSerializable]
    public class ContactInfo
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? City { get; set; }
    }

    // Object with list of simple items
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class Team
    {
        public string Name { get; set; } = "";
        [MarkoutIgnoreInTable]
        public List<string>? Tags { get; set; }
        [MarkoutIgnoreInTable]
        public List<Member>? Members { get; set; }
    }

    [MarkoutSerializable]
    public class Member
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool Active { get; set; }
    }

    // Object with nested object that has a list (2 levels deep)
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class Project
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";

        [MarkoutSection(Name = "Team Info")]
        public TeamInfo? Team { get; set; }
    }

    [MarkoutSerializable]
    public class TeamInfo
    {
        public string? Lead { get; set; }
        public int Size { get; set; }
        [MarkoutIgnoreInTable]
        public List<Contributor>? Contributors { get; set; }
    }

    [MarkoutSerializable]
    public class Contributor
    {
        public string Name { get; set; } = "";
        public int Commits { get; set; }
    }

    // List of objects where each object contains a list (common pattern in dotnet-inspector)
    [MarkoutSerializable(TitleProperty = nameof(PackageName))]
    public class PackageInspection
    {
        [MarkoutPropertyName("Package")]
        public string PackageName { get; set; } = "";
        public string Version { get; set; } = "";

        [MarkoutSection(Name = "Dependencies")]
        public List<DependencyGroup>? Dependencies { get; set; }
    }

    [MarkoutSerializable]
    public class DependencyGroup
    {
        [MarkoutPropertyName("Target Framework")]
        public string TargetFramework { get; set; } = "";

        [MarkoutIgnore]  // Cannot be rendered in table - would need pivot strategy
        public List<Dependency>? Packages { get; set; }
    }

    [MarkoutSerializable]
    public class Dependency
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
    }

    // Deep nesting - 3 levels (edge case to test limits)
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class Organization
    {
        public string Name { get; set; } = "";

        [MarkoutSection(Name = "Departments")]
        public List<Department>? Departments { get; set; }
    }

    [MarkoutSerializable]
    public class Department
    {
        public string Name { get; set; } = "";

        [MarkoutIgnore]  // Cannot be rendered in table - demonstrates deep nesting limitation
        public List<Team2>? Teams { get; set; }
    }

    [MarkoutSerializable]
    public class Team2
    {
        public string Name { get; set; } = "";
        public int MemberCount { get; set; }

        [MarkoutIgnore]  // Cannot be rendered in table - demonstrates deep nesting limitation
        public List<string>? Projects { get; set; }
    }

    // Dictionary-like data (using two-column format)
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class Configuration
    {
        public string Name { get; set; } = "";

        [MarkoutSection(Name = "Settings")]
        public List<Setting>? Settings { get; set; }
    }

    [MarkoutSerializable]
    public class Setting
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    // Multiple lists at same level
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class Application
    {
        public string Name { get; set; } = "";

        [MarkoutSection(Name = "Services")]
        public List<Service>? Services { get; set; }

        [MarkoutSection(Name = "Dependencies")]
        public List<Dependency>? Dependencies { get; set; }

        [MarkoutIgnoreInTable]
        public List<string>? Features { get; set; }
    }

    [MarkoutSerializable]
    public class Service
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public bool Enabled { get; set; }
    }

    // Self-referential type — tests cycle protection in the parser
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class OrgNode
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";

        [MarkoutIgnoreInTable]
        public OrgNode? Manager { get; set; }
    }

    // Mutual recursion — A references B, B references A
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class CycleDepartment
    {
        public string Name { get; set; } = "";

        [MarkoutIgnoreInTable]
        [MarkoutSection(Name = "Lead")]
        public Employee? Lead { get; set; }
    }

    [MarkoutSerializable]
    public class Employee
    {
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";

        [MarkoutIgnoreInTable]
        [MarkoutSection(Name = "Department")]
        public CycleDepartment? Department { get; set; }
    }

    // Deep nesting — 6+ levels to test heading cap
    [MarkoutSerializable(TitleProperty = nameof(Name))]
    public class Level1
    {
        public string Name { get; set; } = "";
        [MarkoutSection(Name = "Child")]
        public Level2? Child { get; set; }
    }

    [MarkoutSerializable]
    public class Level2
    {
        public string Value2 { get; set; } = "";
        [MarkoutSection(Name = "Child")]
        public Level3? Child { get; set; }
    }

    [MarkoutSerializable]
    public class Level3
    {
        public string Value3 { get; set; } = "";
        [MarkoutSection(Name = "Child")]
        public Level4? Child { get; set; }
    }

    [MarkoutSerializable]
    public class Level4
    {
        public string Value4 { get; set; } = "";
        [MarkoutSection(Name = "Child")]
        public Level5? Child { get; set; }
    }

    [MarkoutSerializable]
    public class Level5
    {
        public string Value5 { get; set; } = "";
        [MarkoutSection(Name = "Child")]
        public Level6? Child { get; set; }
    }

    [MarkoutSerializable]
    public class Level6
    {
        public string Value6 { get; set; } = "";
        [MarkoutSection(Name = "Child")]
        public Level7? Child { get; set; }
    }

    [MarkoutSerializable]
    public class Level7
    {
        public string Value7 { get; set; } = "";
    }

    // Nested object with NamingPolicy and FieldLayout on the nested type
    [MarkoutSerializable(TitleProperty = nameof(FileName))]
    public class LibraryReport
    {
        public string FileName { get; set; } = "";

        [MarkoutSection(Name = "Library Info")]
        public LibraryInfoSection? Info { get; set; }
    }

    [MarkoutSerializable(NamingPolicy = NamingPolicy.PascalCaseWords, FieldLayout = FieldLayout.Table)]
    [MarkoutSkipNull]
    public class LibraryInfoSection
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? InformationalVersion { get; set; }
        public string? AssemblyVersion { get; set; }
        public string? TargetFramework { get; set; }
        [MarkoutBoolFormat("Yes", "No")]
        public bool Deterministic { get; set; }
        [MarkoutBoolFormat("Yes", "No")]
        public bool Reproducible { get; set; }
    }

    // Nested object in a collection with NamingPolicy
    [MarkoutSerializable(NamingPolicy = NamingPolicy.PascalCaseWords)]
    public class AttributeRow
    {
        public string Name { get; set; } = "";
        public string TargetKind { get; set; } = "";
        public string AttributeValue { get; set; } = "";
    }

    [MarkoutSerializable(TitleProperty = nameof(Title))]
    public class AttributeReport
    {
        public string Title { get; set; } = "";

        [MarkoutSection(Name = "Custom Attributes")]
        public List<AttributeRow>? Attributes { get; set; }
    }

#endregion

#region Nested Structure Test Context

[MarkoutContext(typeof(Person))]
[MarkoutContext(typeof(Team))]
[MarkoutContext(typeof(Project))]
[MarkoutContext(typeof(PackageInspection))]
[MarkoutContext(typeof(Organization))]
[MarkoutContext(typeof(Configuration))]
[MarkoutContext(typeof(Application))]
[MarkoutContext(typeof(OrgNode))]
[MarkoutContext(typeof(CycleDepartment))]
[MarkoutContext(typeof(Level1))]
[MarkoutContext(typeof(LibraryReport))]
[MarkoutContext(typeof(AttributeReport))]
public partial class NestedTestContext : MarkoutSerializerContext
{
}

#endregion

/// <summary>
/// Tests for nested data structures: objects containing lists, lists containing objects with lists, etc.
/// These tests help define and validate the limits of nesting in the Markout format.
/// </summary>
public class NestedStructureTests
{
    #region Level 1: Object with Nested Object

    [Fact]
    public void Serialize_ObjectWithNestedObject_CreatesSubsection()
    {
        var person = new Person
        {
            Name = "John Doe",
            Age = 30,
            Contact = new ContactInfo
            {
                Email = "john@example.com",
                Phone = "555-1234",
                City = "Seattle"
            }
        };

        var mdf = MarkoutSerializer.Serialize(person, NestedTestContext.Default);

        // Should have title
        Assert.Contains("# John Doe", mdf);

        // Should have top-level fields
        Assert.Contains("| Age | 30 |", mdf);

        // Should have section for nested object
        Assert.Contains("## Contact Info", mdf);

        // Should have nested object fields
        Assert.Contains("| Email | john@example.com |", mdf);
        Assert.Contains("| Phone | 555-1234 |", mdf);
        Assert.Contains("| City | Seattle |", mdf);
    }

    [Fact]
    public void Serialize_ObjectWithNullNestedObject_OmitsSection()
    {
        var person = new Person
        {
            Name = "Jane Doe",
            Age = 25,
            Contact = null
        };

        var mdf = MarkoutSerializer.Serialize(person, NestedTestContext.Default);

        Assert.Contains("# Jane Doe", mdf);
        Assert.Contains("| Age | 25 |", mdf);
        Assert.DoesNotContain("## Contact Info", mdf);
    }

    #endregion

    #region Level 1: Object with List of Objects (Table Format)

    [Fact]
    public void Serialize_ObjectWithListOfObjects_CreatesTable()
    {
        var team = new Team
        {
            Name = "Engineering",
            Tags = new List<string> { "backend", "frontend", "devops" },
            Members = new List<Member>
            {
                new Member { Name = "Alice", Role = "Engineer", Active = true },
                new Member { Name = "Bob", Role = "Lead", Active = true },
                new Member { Name = "Charlie", Role = "Engineer", Active = false }
            }
        };

        var mdf = MarkoutSerializer.Serialize(team, NestedTestContext.Default);

        // Title
        Assert.Contains("# Engineering", mdf);

        // String array
        Assert.Contains("Tags:", mdf);
        Assert.Contains("- backend", mdf);
        Assert.Contains("- frontend", mdf);
        Assert.Contains("- devops", mdf);

        // Table for complex objects
        Assert.Contains("## Members", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Role |", mdf);
        Assert.Contains("| Active |", mdf);
        Assert.Contains("| Alice |", mdf);
        Assert.Contains("| Bob |", mdf);
        Assert.Contains("| Charlie |", mdf);
    }

    [Fact]
    public void Serialize_ObjectWithEmptyList_OmitsSection()
    {
        var team = new Team
        {
            Name = "Empty Team",
            Members = new List<Member>()
        };

        var mdf = MarkoutSerializer.Serialize(team, NestedTestContext.Default);

        Assert.Contains("# Empty Team", mdf);
        // Empty lists should not create sections
        Assert.DoesNotContain("## Members", mdf);
    }

    #endregion

    #region Level 2: Object -> Nested Object -> List

    [Fact]
    public void Serialize_NestedObjectWithList_CreatesSubsectionWithTable()
    {
        var project = new Project
        {
            Name = "Markout Library",
            Version = "1.0.0",
            Team = new TeamInfo
            {
                Lead = "Alice",
                Size = 3,
                Contributors = new List<Contributor>
                {
                    new Contributor { Name = "Bob", Commits = 42 },
                    new Contributor { Name = "Charlie", Commits = 38 }
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(project, NestedTestContext.Default);

        // Title and top-level fields
        Assert.Contains("# Markout Library", mdf);
        Assert.Contains("| Version | 1.0.0 |", mdf);

        // Nested object section
        Assert.Contains("## Team Info", mdf);
        Assert.Contains("| Lead | Alice |", mdf);
        Assert.Contains("| Size | 3 |", mdf);

        // List within nested object - should become table
        Assert.Contains("## Contributors", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Commits |", mdf);
        Assert.Contains("| Bob |", mdf);
        Assert.Contains("| Charlie |", mdf);
    }

    #endregion

    #region Level 2: List of Objects (Each with Nested List)

    [Fact]
    public void Serialize_ListOfObjectsWithNestedLists_CreatesMultipleSections()
    {
        var package = new PackageInspection
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            Dependencies = new List<DependencyGroup>
            {
                new DependencyGroup
                {
                    TargetFramework = "net6.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" }
                    }
                },
                new DependencyGroup
                {
                    TargetFramework = "net8.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" }
                    }
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(package, NestedTestContext.Default);

        // Title
        Assert.Contains("# Newtonsoft.Json", mdf);
        Assert.Contains("| Version | 13.0.3 |", mdf);

        // Main section for dependencies
        Assert.Contains("## Dependencies", mdf);

        // Should have table with framework info
        Assert.Contains("| Target Framework |", mdf);
        Assert.Contains("| net6.0 |", mdf);
        Assert.Contains("| net8.0 |", mdf);

        // Question: How should nested lists be represented?
        // This is a key design decision for the format
        // Currently, nested lists within table items cannot be shown in table format
    }

    #endregion

    #region Level 3: Deep Nesting (Edge Case)

    [Fact]
    public void Serialize_ThreeLevelNesting_HandlesGracefully()
    {
        var org = new Organization
        {
            Name = "TechCorp",
            Departments = new List<Department>
            {
                new Department
                {
                    Name = "Engineering",
                    Teams = new List<Team2>
                    {
                        new Team2
                        {
                            Name = "Backend",
                            MemberCount = 5,
                            Projects = new List<string> { "API", "Database", "Auth" }
                        },
                        new Team2
                        {
                            Name = "Frontend",
                            MemberCount = 3,
                            Projects = new List<string> { "WebApp", "MobileApp" }
                        }
                    }
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(org, NestedTestContext.Default);

        // Title
        Assert.Contains("# TechCorp", mdf);

        // Level 1: Departments section
        Assert.Contains("## Departments", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Engineering |", mdf);

        // Level 2: Teams within Department
        // This is problematic - tables can't contain nested lists
        // The format likely needs constraints or a different representation
    }

    #endregion

    #region Dictionary-Like Data (Key-Value Pairs)

    [Fact]
    public void Serialize_ConfigurationSettings_UsesTableFormat()
    {
        var config = new Configuration
        {
            Name = "Production",
            Settings = new List<Setting>
            {
                new Setting { Key = "DatabaseUrl", Value = "postgresql://localhost:5432/db" },
                new Setting { Key = "CacheTimeout", Value = "3600" },
                new Setting { Key = "LogLevel", Value = "Info" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(config, NestedTestContext.Default);

        Assert.Contains("# Production", mdf);
        Assert.Contains("## Settings", mdf);

        // Should use table format (not simple pairs)
        Assert.Contains("| Key |", mdf);
        Assert.Contains("| Value |", mdf);
        Assert.Contains("| DatabaseUrl |", mdf);
        Assert.Contains("| CacheTimeout |", mdf);
    }

    #endregion

    #region Multiple Lists at Same Level

    [Fact]
    public void Serialize_MultipleLists_CreatesMultipleSections()
    {
        var app = new Application
        {
            Name = "MyApp",
            Services = new List<Service>
            {
                new Service { Name = "API", Url = "https://api.example.com", Enabled = true },
                new Service { Name = "Cache", Url = "redis://cache:6379", Enabled = true }
            },
            Dependencies = new List<Dependency>
            {
                new Dependency { Name = "React", Version = "18.0.0" },
                new Dependency { Name = "TypeScript", Version = "5.0.0" }
            },
            Features = new List<string> { "Auth", "Payments", "Analytics" }
        };

        var mdf = MarkoutSerializer.Serialize(app, NestedTestContext.Default);

        // Title
        Assert.Contains("# MyApp", mdf);

        // String array
        Assert.Contains("Features:", mdf);
        Assert.Contains("- Auth", mdf);

        // First complex list
        Assert.Contains("## Services", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Url |", mdf);
        Assert.Contains("| API |", mdf);

        // Second complex list
        Assert.Contains("## Dependencies", mdf);
        Assert.Contains("| React |", mdf);
        Assert.Contains("| TypeScript |", mdf);
    }

    #endregion

    #region Edge Cases: Null and Empty Collections

    [Fact]
    public void Serialize_NullLists_OmitsAllSections()
    {
        var app = new Application
        {
            Name = "MinimalApp",
            Services = null,
            Dependencies = null,
            Features = null
        };

        var mdf = MarkoutSerializer.Serialize(app, NestedTestContext.Default);

        Assert.Contains("# MinimalApp", mdf);
        Assert.DoesNotContain("## Services", mdf);
        Assert.DoesNotContain("## Dependencies", mdf);
        Assert.DoesNotContain("Features:", mdf);
    }

    [Fact]
    public void Serialize_EmptyNestedObject_CreatesEmptySection()
    {
        var person = new Person
        {
            Name = "Test",
            Age = 20,
            Contact = new ContactInfo()
        };

        var mdf = MarkoutSerializer.Serialize(person, NestedTestContext.Default);

        // Should still create section even if all fields are null/default
        Assert.Contains("## Contact Info", mdf);
    }

    #endregion

    #region Cycle Protection and Heading Depth

    [Fact]
    public void SelfReferentialType_RendersOneLevel_StopsAtCycle()
    {
        var node = new OrgNode
        {
            Name = "Alice",
            Role = "Engineer",
            Manager = new OrgNode { Name = "Bob", Role = "Director" }
        };

        var mdf = MarkoutSerializer.Serialize(node, NestedTestContext.Default);

        // Root renders with title and field
        Assert.Contains("# Alice", mdf);
        Assert.Contains("| Role | Engineer |", mdf);

        // Nested Manager renders as section with fields
        Assert.Contains("## Manager", mdf);
        Assert.Contains("| Name | Bob |", mdf);
        Assert.Contains("| Role | Director |", mdf);

        // Manager.Manager is NOT rendered (cycle stopped)
        // Only one "## Manager" heading should exist
        Assert.Equal(1, mdf.Split("## Manager").Length - 1);
    }

    [Fact]
    public void SelfReferentialType_NullBackReference_OmitsSection()
    {
        var node = new OrgNode
        {
            Name = "Alice",
            Role = "Engineer",
            Manager = null
        };

        var mdf = MarkoutSerializer.Serialize(node, NestedTestContext.Default);

        Assert.Contains("# Alice", mdf);
        Assert.DoesNotContain("## Manager", mdf);
    }

    [Fact]
    public void MutualRecursion_DepartmentToEmployee_StopsBeforeInfiniteLoop()
    {
        // CycleDepartment → Employee → CycleDepartment → Employee (stopped)
        var dept = new CycleDepartment
        {
            Name = "Engineering",
            Lead = new Employee
            {
                Name = "Alice",
                Title = "VP Engineering",
                Department = new CycleDepartment
                {
                    Name = "Inner Dept",
                    Lead = new Employee { Name = "Should not appear", Title = "Ghost" }
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(dept, NestedTestContext.Default);

        // Root CycleDepartment renders
        Assert.Contains("# Engineering", mdf);

        // Employee section renders with fields
        Assert.Contains("## Lead", mdf);
        Assert.Contains("| Name | Alice |", mdf);
        Assert.Contains("| Title | VP Engineering |", mdf);

        // Inner CycleDepartment renders one level (name only)
        Assert.Contains("Inner Dept", mdf);

        // But the inner Employee (Lead inside inner CycleDepartment) is stopped by cycle guard
        Assert.DoesNotContain("Should not appear", mdf);
    }

    [Fact]
    public void DeepNesting_HeadingsCapAtH6()
    {
        var data = new Level1
        {
            Name = "Root",
            Child = new Level2
            {
                Value2 = "L2",
                Child = new Level3
                {
                    Value3 = "L3",
                    Child = new Level4
                    {
                        Value4 = "L4",
                        Child = new Level5
                        {
                            Value5 = "L5",
                            Child = new Level6
                            {
                                Value6 = "L6",
                                Child = new Level7 { Value7 = "L7" }
                            }
                        }
                    }
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(data, NestedTestContext.Default);

        // All values should be present
        Assert.Contains("# Root", mdf);      // H1 — root title
        Assert.Contains("| Value2 | L2 |", mdf);
        Assert.Contains("| Value3 | L3 |", mdf);
        Assert.Contains("| Value4 | L4 |", mdf);
        Assert.Contains("| Value5 | L5 |", mdf);
        Assert.Contains("| Value6 | L6 |", mdf);
        Assert.Contains("| Value7 | L7 |", mdf);

        // Heading levels: H1 (root), H2 (Child), H3 (Child), H4, H5, H6
        // H7 should NOT exist — capped at H6
        Assert.DoesNotContain("####### ", mdf);

        // H6 should exist (level 5's Child section or level 6's Child section)
        Assert.Contains("###### Child", mdf);
    }

    #endregion

    #region Multi-TFM Wrapper Pattern: Collection with FieldCollection elements

    [Fact]
    public void Serialize_CollectionWithFieldCollectionElements_RendersSubsections()
    {
        var report = new MultiTfmAuditReport
        {
            Title = "System.Text.Json",
            Items =
            [
                new AuditItem
                {
                    Name = "System.Text.Json.dll",
                    Framework = "net9.0",
                    SomeField = "should not appear",
                    InfoSection = [new("Name", "System.Text.Json"), new("Version", "9.0.0")]
                },
                new AuditItem
                {
                    Name = "System.Text.Json.dll",
                    Framework = "net8.0",
                    SomeField = "should not appear",
                    InfoSection = [new("Name", "System.Text.Json"), new("Version", "8.0.0")]
                }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, MultiTfmAuditReportContext.Default);

        // Single H1 from wrapper
        Assert.Single(mdf.Split('\n'), l => l.StartsWith("# "));
        Assert.Contains("# System.Text.Json", mdf);

        // H2 section heading
        Assert.Contains("## Items", mdf);

        // H3 subsection per item with context
        Assert.Contains("### System.Text.Json.dll (net9.0)", mdf);
        Assert.Contains("### System.Text.Json.dll (net8.0)", mdf);

        // H4 item sections (Info within each item)
        Assert.Contains("#### Info", mdf);

        // FieldCollection content renders as field table
        Assert.Contains("| Name | System.Text.Json |", mdf);
        Assert.Contains("| Version | 9.0.0 |", mdf);
        Assert.Contains("| Version | 8.0.0 |", mdf);
    }

    [Fact]
    public void Serialize_CollectionElement_AutoFieldsFalse_SuppressesScalarFields()
    {
        var report = new MultiTfmAuditReport
        {
            Title = "Test",
            Items =
            [
                new AuditItem
                {
                    Name = "test.dll",
                    Framework = "net9.0",
                    SomeField = "this should not appear",
                    InfoSection = [new("Key", "Value")]
                }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, MultiTfmAuditReportContext.Default);

        // SomeField should NOT appear because AutoFields=false on AuditItem
        Assert.DoesNotContain("SomeField", mdf);
        Assert.DoesNotContain("this should not appear", mdf);
    }

    [Fact]
    public void Serialize_CollectionElement_TitleContextProperty_RendersInParentheses()
    {
        var report = new MultiTfmAuditReport
        {
            Title = "Test",
            Items =
            [
                new AuditItem
                {
                    Name = "Foo.dll",
                    Framework = "net9.0",
                    InfoSection = [new("A", "B")]
                }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, MultiTfmAuditReportContext.Default);

        // Title with context renders as "### Foo.dll (net9.0)"
        Assert.Contains("### Foo.dll (net9.0)", mdf);
    }

    [Fact]
    public void Serialize_CollectionElement_NullContext_RendersWithoutParentheses()
    {
        var report = new MultiTfmAuditReport
        {
            Title = "Test",
            Items =
            [
                new AuditItem
                {
                    Name = "Foo.dll",
                    Framework = null,
                    InfoSection = [new("A", "B")]
                }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, MultiTfmAuditReportContext.Default);

        // No context — should render without parentheses
        Assert.Contains("### Foo.dll", mdf);
        Assert.DoesNotContain("### Foo.dll (", mdf);
    }

    #endregion
}

#region Multi-TFM Wrapper Pattern: Collection with FieldCollection Elements

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class MultiTfmAuditReport
{
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Items")]
    public List<AuditItem>? Items { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), TitleContextProperty = nameof(Framework), AutoFields = false)]
public class AuditItem
{
    public string Name { get; set; } = "";
    public string? Framework { get; set; }
    public string SomeField { get; set; } = "";

    [MarkoutSection(Name = "Info")]
    public List<MarkoutField> InfoSection { get; set; } = [];
}

[MarkoutContext(typeof(MultiTfmAuditReport))]
public partial class MultiTfmAuditReportContext : MarkoutSerializerContext
{
}

#endregion

#region Real-World Pattern: DotNet Inspector Structure

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class AssemblyTest
{
    public string Name { get; set; } = "";
    public string Architecture { get; set; } = "";
    public bool Signed { get; set; }

    [MarkoutSection(Name = "Assembly Info")]
    public AssemblyMetadata? Metadata { get; set; }

    [MarkoutSection(Name = "API Surface")]
    public ApiInfo? Api { get; set; }
}

[MarkoutSerializable]
public class AssemblyMetadata
{
    [MarkoutPropertyName("Assembly Version")]
    public string? AssemblyVersion { get; set; }

    [MarkoutPropertyName("File Version")]
    public string? FileVersion { get; set; }

    [MarkoutPropertyName("Target Framework")]
    public string? TargetFramework { get; set; }
}

[MarkoutSerializable]
public class ApiInfo
{
    [MarkoutPropertyName("Public Types")]
    public int PublicTypes { get; set; }

    [MarkoutPropertyName("Public Methods")]
    public int PublicMethods { get; set; }

    // Note: In real dotnet-inspector, this has [MarkoutIgnore]
    // because showing full API in table format would be too verbose
    [MarkoutIgnore]
    public List<string>? TypeNames { get; set; }
}

[MarkoutContext(typeof(AssemblyTest))]
public partial class RealWorldTestContext : MarkoutSerializerContext
{
}

#endregion

public class NestedStructureRealWorldTests
{

    [Fact]
    public void Serialize_RealWorldPattern_MultipleNestedObjects()
    {
        var assembly = new AssemblyTest
        {
            Name = "System.Text.Json",
            Architecture = "AnyCPU",
            Signed = true,
            Metadata = new AssemblyMetadata
            {
                AssemblyVersion = "6.0.0.0",
                FileVersion = "6.0.21.52210",
                TargetFramework = "net6.0"
            },
            Api = new ApiInfo
            {
                PublicTypes = 144,
                PublicMethods = 892
            }
        };

        var mdf = MarkoutSerializer.Serialize(assembly, RealWorldTestContext.Default);

        // Title
        Assert.Contains("# System.Text.Json", mdf);

        // Top-level fields
        Assert.Contains("| Architecture | AnyCPU |", mdf);
        Assert.Contains("| Signed | yes |", mdf);

        // First nested section
        Assert.Contains("## Assembly Info", mdf);
        Assert.Contains("| Assembly Version | 6.0.0.0 |", mdf);
        Assert.Contains("| File Version | 6.0.21.52210 |", mdf);
        Assert.Contains("| Target Framework | net6.0 |", mdf);

        // Second nested section
        Assert.Contains("## API Surface", mdf);
        Assert.Contains("| Public Types | 144 |", mdf);
        Assert.Contains("| Public Methods | 892 |", mdf);
    }
}

#region CodeSection Models

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ConstructorOverload
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    [MarkoutIgnoreInTable]
    public CodeSection Signature { get; set; }

    [MarkoutSection(Name = "Parameters")]
    public List<ParameterRow>? Parameters { get; set; }
}

[MarkoutSerializable]
public class ParameterRow
{
    public string Parameter { get; set; } = "";
    public string Type { get; set; } = "";
    public string Notes { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ApiMethodView
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Kind { get; set; } = "";
    [MarkoutIgnoreInTable]
    public CodeSection Source { get; set; }
}

[MarkoutContext(typeof(ConstructorOverload))]
[MarkoutContext(typeof(ApiMethodView))]
[MarkoutContext(typeof(ILView))]
[MarkoutContext(typeof(VulnerabilityReport))]
[MarkoutContext(typeof(CtorEmphasisView))]
public partial class CodeSectionTestContext : MarkoutSerializerContext
{
}

// Sectioned CodeSection: [MarkoutSection] on CodeSection renders heading + code block
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ILView
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Kind { get; set; } = "";

    [MarkoutSection(Name = "IL")]
    [MarkoutIgnoreInTable]
    public CodeSection IlCode { get; set; }

    [MarkoutSection(Name = "Source")]
    [MarkoutIgnoreInTable]
    public CodeSection SourceCode { get; set; }
}

// Callout as property type
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class VulnerabilityReport
{
    [MarkoutIgnore] public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    [MarkoutIgnoreInTable]
    public Callout Alert { get; set; }

    [MarkoutSection(Name = "Vulnerabilities")]
    public List<VulnRow>? Vulnerabilities { get; set; }
}

[MarkoutSerializable]
public class VulnRow
{
    public string Severity { get; set; } = "";
    public string Advisory { get; set; } = "";
}

// Nested ctor overloads: List<ConstructorOverload> with CodeSection + table
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CtorEmphasisView
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Kind { get; set; } = "";

    [MarkoutSection(Name = "Constructors")]
    public List<ConstructorOverload>? Overloads { get; set; }
}

// Distribution: List<Breakdown> as section for CVE severity breakdown
[MarkoutSerializable(TitleProperty = nameof(Framework))]
public class CveSummary
{
    [MarkoutIgnore] public string Framework { get; set; } = "";
    public string TotalCves { get; set; } = "";

    [MarkoutSection(Name = "Severity Distribution")]
    [MarkoutIgnoreInTable]
    public List<Breakdown>? Distribution { get; set; }
}

[MarkoutContext(typeof(CveSummary))]
public partial class DistributionTestContext : MarkoutSerializerContext
{
}

// Multi-column IgnoreProperty: comma-separated property names
[MarkoutSerializable]
public record ApiMemberRow(string? Select, string Name, string Signature, string? Description);

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class MultiIgnoreView
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Members", IgnoreProperty = "Description,Select")]
    public List<ApiMemberRow>? MembersCompact { get; set; }

    [MarkoutSection(Name = "Members", IgnoreProperty = "Select")]
    public List<ApiMemberRow>? MembersWithDocs { get; set; }

    [MarkoutSection(Name = "Members", IgnoreProperty = "Description")]
    public List<ApiMemberRow>? MembersWithSelect { get; set; }

    [MarkoutSection(Name = "Members")]
    public List<ApiMemberRow>? MembersAll { get; set; }
}

[MarkoutContext(typeof(MultiIgnoreView))]
public partial class MultiIgnoreTestContext : MarkoutSerializerContext
{
}

// GroupBy: items grouped by a property, each group gets a subheading
[MarkoutSerializable]
public record DiffChange(
    [property: MarkoutIgnore] string TypeName,
    string Message);

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class DiffReport
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Versions { get; set; } = "";

    [MarkoutSection(Name = "Breaking Changes", GroupBy = nameof(DiffChange.TypeName))]
    public List<DiffChange>? BreakingChanges { get; set; }

    [MarkoutSection(Name = "Additive Changes", GroupBy = nameof(DiffChange.TypeName))]
    public List<DiffChange>? AdditiveChanges { get; set; }
}

// GroupBy with table rows (multiple visible properties)
[MarkoutSerializable]
public record GroupedMetric(
    [property: MarkoutIgnore] string Category,
    string Name,
    string Value);

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class MetricReport
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Metrics", GroupBy = nameof(GroupedMetric.Category))]
    public List<GroupedMetric>? Metrics { get; set; }
}

[MarkoutContext(typeof(DiffReport))]
[MarkoutContext(typeof(MetricReport))]
public partial class GroupByTestContext : MarkoutSerializerContext
{
}

// Nested callout + section table: collection elements with Callout property and [MarkoutSection] table
// This tests that HasNestedContent recognizes Callout/Section and triggers subsection-per-item rendering

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class VerificationReport
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Generated { get; set; } = "";

    [MarkoutSection(Name = "Families")]
    public List<FamilyReport>? Families { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class FamilyReport
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutSection(Name = "Distros")]
    public List<DistroReport>? Distros { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class DistroReport
{
    [MarkoutIgnore] public string Name { get; set; } = "";
    [MarkoutIgnoreInTable]
    public Callout Alert { get; set; }

    [MarkoutSection(Name = "Issues")]
    public List<IssueRow>? Issues { get; set; }
}

[MarkoutSerializable]
public record IssueRow(string Version, [property: MarkoutPropertyName("EOL Date")] string EolDate);

[MarkoutContext(typeof(VerificationReport))]
[MarkoutContext(typeof(IssueRow))]
public partial class NestedCalloutTestContext : MarkoutSerializerContext
{
}

// MarkoutUnwrap: collection items rendered inline without parent heading

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class UnwrapReport
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Generated { get; set; } = "";

    [MarkoutUnwrap]
    public List<UnwrapFamily>? Families { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class UnwrapFamily
{
    [MarkoutIgnore] public string Name { get; set; } = "";

    [MarkoutUnwrap]
    public List<UnwrapDistro>? Distros { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class UnwrapDistro
{
    [MarkoutIgnore] public string Name { get; set; } = "";
    [MarkoutIgnoreInTable]
    public Callout Alert { get; set; }

    [MarkoutSection(Name = "Issues")]
    public List<IssueRow>? Issues { get; set; }
}

[MarkoutContext(typeof(UnwrapReport))]
public partial class UnwrapTestContext : MarkoutSerializerContext
{
}

#endregion

public class CodeSectionTests
{
    [Fact]
    public void Serialize_CodeSection_RendersCodeBlock()
    {
        var view = new ApiMethodView
        {
            Title = "JsonSerializer.Serialize",
            Kind = "Method",
            Source = new CodeSection("csharp", "public static string Serialize(object? value);")
        };

        var mdf = MarkoutSerializer.Serialize(view, CodeSectionTestContext.Default);

        Assert.Contains("# JsonSerializer.Serialize", mdf);
        Assert.Contains("| Kind | Method |", mdf);
        Assert.Contains("```csharp", mdf);
        Assert.Contains("public static string Serialize(object? value);", mdf);
        Assert.Contains("```", mdf);
    }

    [Fact]
    public void Serialize_CodeSection_WithNestedTable()
    {
        var overload = new ConstructorOverload
        {
            Title = "Overload 1: 2 parameters",
            Signature = new CodeSection("csharp", "new JsonSerializerOptions(JsonSerializerDefaults defaults)"),
            Parameters =
            [
                new ParameterRow { Parameter = "defaults", Type = "JsonSerializerDefaults", Notes = "required" },
                new ParameterRow { Parameter = "options", Type = "JsonSerializerOptions?", Notes = "optional" }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(overload, CodeSectionTestContext.Default);

        Assert.Contains("# Overload 1: 2 parameters", mdf);
        Assert.Contains("```csharp", mdf);
        Assert.Contains("new JsonSerializerOptions", mdf);
        Assert.Contains("```", mdf);
        Assert.Contains("## Parameters", mdf);
        Assert.Contains("defaults", mdf);
        Assert.Contains("JsonSerializerDefaults", mdf);
    }

    [Fact]
    public void Serialize_CodeSection_DefaultContent_SkipsBlock()
    {
        var view = new ApiMethodView
        {
            Title = "Test",
            Kind = "Method",
            Source = default  // Content is null
        };

        var mdf = MarkoutSerializer.Serialize(view, CodeSectionTestContext.Default);

        Assert.Contains("# Test", mdf);
        Assert.Contains("| Kind | Method |", mdf);
        Assert.DoesNotContain("```", mdf);
    }

    [Fact]
    public void Serialize_SectionedCodeSection_RendersHeadingAndCodeBlock()
    {
        var view = new ILView
        {
            Title = "JsonSerializer.Serialize",
            Kind = "Method",
            IlCode = new CodeSection("il", ".method public static string Serialize(object)"),
            SourceCode = new CodeSection("csharp", "public static string Serialize(object? value) => ...")
        };

        var mdf = MarkoutSerializer.Serialize(view, CodeSectionTestContext.Default);

        Assert.Contains("# JsonSerializer.Serialize", mdf);
        Assert.Contains("| Kind | Method |", mdf);

        // IL section with heading + code block
        Assert.Contains("## IL", mdf);
        Assert.Contains("```il", mdf);
        Assert.Contains(".method public static string Serialize(object)", mdf);

        // Source section with heading + code block
        Assert.Contains("## Source", mdf);
        Assert.Contains("```csharp", mdf);
        Assert.Contains("public static string Serialize(object? value) => ...", mdf);
    }

    [Fact]
    public void Serialize_SectionedCodeSection_SkipsWhenDefault()
    {
        var view = new ILView
        {
            Title = "Test",
            Kind = "Method",
            IlCode = default,
            SourceCode = default
        };

        var mdf = MarkoutSerializer.Serialize(view, CodeSectionTestContext.Default);

        Assert.Contains("# Test", mdf);
        Assert.DoesNotContain("## IL", mdf);
        Assert.DoesNotContain("## Source", mdf);
        Assert.DoesNotContain("```", mdf);
    }

    [Fact]
    public void Serialize_CalloutProperty_RendersCallout()
    {
        var report = new VulnerabilityReport
        {
            Name = "System.Text.Json",
            Version = "6.0.0",
            Alert = new Callout(CalloutSeverity.Warning, "3 known vulnerabilities found."),
            Vulnerabilities =
            [
                new VulnRow { Severity = "High", Advisory = "CVE-2024-1234" },
                new VulnRow { Severity = "Medium", Advisory = "CVE-2024-5678" }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, CodeSectionTestContext.Default);

        Assert.Contains("# System.Text.Json", mdf);
        Assert.Contains("| Version | 6.0.0 |", mdf);

        // Callout rendered in markdown format
        Assert.Contains("> [!WARNING]", mdf);
        Assert.Contains("> 3 known vulnerabilities found.", mdf);

        // Vulnerabilities table
        Assert.Contains("## Vulnerabilities", mdf);
        Assert.Contains("CVE-2024-1234", mdf);
    }

    [Fact]
    public void Serialize_CalloutProperty_SkipsWhenDefault()
    {
        var report = new VulnerabilityReport
        {
            Name = "Safe.Package",
            Version = "1.0.0",
            Alert = default  // Message is null
        };

        var mdf = MarkoutSerializer.Serialize(report, CodeSectionTestContext.Default);

        Assert.Contains("# Safe.Package", mdf);
        Assert.DoesNotContain("[!WARNING]", mdf);
        Assert.DoesNotContain("WARNING", mdf);
    }

    [Fact]
    public void Serialize_NestedCtorOverloads_RendersSubsections()
    {
        var view = new CtorEmphasisView
        {
            Title = "JsonSerializerOptions",
            Kind = "Class",
            Overloads =
            [
                new ConstructorOverload
                {
                    Title = "Overload 1: 0 parameters",
                    Signature = new CodeSection("csharp", "new JsonSerializerOptions()"),
                },
                new ConstructorOverload
                {
                    Title = "Overload 2: 1 parameter",
                    Signature = new CodeSection("csharp", "new JsonSerializerOptions(JsonSerializerDefaults defaults)"),
                    Parameters =
                    [
                        new ParameterRow { Parameter = "defaults", Type = "JsonSerializerDefaults", Notes = "required" }
                    ]
                }
            ]
        };

        var mdf = MarkoutSerializer.Serialize(view, CodeSectionTestContext.Default);

        // Top level
        Assert.Contains("# JsonSerializerOptions", mdf);
        Assert.Contains("| Kind | Class |", mdf);

        // Section heading
        Assert.Contains("## Constructors", mdf);

        // Subsection per overload
        Assert.Contains("### Overload 1: 0 parameters", mdf);
        Assert.Contains("new JsonSerializerOptions()", mdf);

        Assert.Contains("### Overload 2: 1 parameter", mdf);
        Assert.Contains("new JsonSerializerOptions(JsonSerializerDefaults defaults)", mdf);

        // Nested parameter table
        Assert.Contains("#### Parameters", mdf);
        Assert.Contains("defaults", mdf);
        Assert.Contains("JsonSerializerDefaults", mdf);
    }
}

public class DistributionTests
{
    [Fact]
    public void Serialize_DistributionProperty_RendersDistributionChart()
    {
        var summary = new CveSummary
        {
            Framework = ".NET 9",
            TotalCves = "12",
            Distribution =
            [
                new("Jan 2025", [new("Critical", 1), new("High", 3), new("Medium", 2)]),
                new("Feb 2025", [new("Critical", 2), new("High", 1)]),
            ]
        };

        var mdf = MarkoutSerializer.Serialize(summary, DistributionTestContext.Default);

        Assert.Contains("# .NET 9", mdf);
        Assert.Contains("| TotalCves | 12 |", mdf);
        Assert.Contains("## Severity Distribution", mdf);
        Assert.Contains("Jan 2025", mdf);
        Assert.Contains("Feb 2025", mdf);
        Assert.Contains("Critical", mdf);
        Assert.Contains("High", mdf);
    }

    [Fact]
    public void Serialize_DistributionProperty_SkipsWhenEmpty()
    {
        var summary = new CveSummary
        {
            Framework = ".NET 8",
            TotalCves = "0",
            Distribution = []
        };

        var mdf = MarkoutSerializer.Serialize(summary, DistributionTestContext.Default);

        Assert.Contains("# .NET 8", mdf);
        Assert.DoesNotContain("Severity Distribution", mdf);
    }
}

public class MultiIgnorePropertyTests
{
    private static readonly List<ApiMemberRow> TestRows =
    [
        new("`Parse:1`", "Parse", "`string Parse(string)`", "Parses input"),
        new("`Parse:2`", "Parse", "`string Parse(ReadOnlySpan<char>)`", "Parses span"),
    ];

    [Fact]
    public void Serialize_IgnoreTwoProperties_ShowsOnlyNameAndSignature()
    {
        var view = new MultiIgnoreView
        {
            Title = "TestType",
            MembersCompact = TestRows
        };

        var mdf = MarkoutSerializer.Serialize(view, MultiIgnoreTestContext.Default);

        Assert.Contains("| Name", mdf);
        Assert.Contains("| Signature", mdf);
        Assert.DoesNotContain("Select", mdf);
        Assert.DoesNotContain("Description", mdf);
    }

    [Fact]
    public void Serialize_IgnoreSelect_ShowsNameSignatureDescription()
    {
        var view = new MultiIgnoreView
        {
            Title = "TestType",
            MembersWithDocs = TestRows
        };

        var mdf = MarkoutSerializer.Serialize(view, MultiIgnoreTestContext.Default);

        Assert.Contains("| Name", mdf);
        Assert.Contains("| Signature", mdf);
        Assert.Contains("| Description", mdf);
        Assert.DoesNotContain("Select", mdf);
        Assert.Contains("Parses input", mdf);
    }

    [Fact]
    public void Serialize_IgnoreDescription_ShowsSelectNameSignature()
    {
        var view = new MultiIgnoreView
        {
            Title = "TestType",
            MembersWithSelect = TestRows
        };

        var mdf = MarkoutSerializer.Serialize(view, MultiIgnoreTestContext.Default);

        Assert.Contains("| Select", mdf);
        Assert.Contains("| Name", mdf);
        Assert.Contains("| Signature", mdf);
        Assert.DoesNotContain("Description", mdf);
        Assert.Contains("`Parse:1`", mdf);
    }

    [Fact]
    public void Serialize_NoIgnore_ShowsAllColumns()
    {
        var view = new MultiIgnoreView
        {
            Title = "TestType",
            MembersAll = TestRows
        };

        var mdf = MarkoutSerializer.Serialize(view, MultiIgnoreTestContext.Default);

        Assert.Contains("| Select", mdf);
        Assert.Contains("| Name", mdf);
        Assert.Contains("| Signature", mdf);
        Assert.Contains("| Description", mdf);
        Assert.Contains("`Parse:1`", mdf);
        Assert.Contains("Parses input", mdf);
    }
}

public class GroupByTests
{
    [Fact]
    public void Serialize_GroupBy_SingleProperty_RendersListItemsPerGroup()
    {
        var report = new DiffReport
        {
            Title = "API Diff: MyLib",
            Versions = "1.0 → 2.0",
            BreakingChanges =
            [
                new("StringBuilder", "Method removed: Clear()"),
                new("StringBuilder", "Property changed: Length"),
                new("String", "Method removed: Copy()"),
            ],
            AdditiveChanges =
            [
                new("String", "Method added: ReplaceLineEndings()"),
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, GroupByTestContext.Default);

        // Section heading
        Assert.Contains("## Breaking Changes", mdf);

        // Group subheadings
        Assert.Contains("### StringBuilder", mdf);
        Assert.Contains("### String", mdf);

        // List items under groups
        Assert.Contains("Method removed: Clear()", mdf);
        Assert.Contains("Property changed: Length", mdf);
        Assert.Contains("Method removed: Copy()", mdf);

        // Additive section
        Assert.Contains("## Additive Changes", mdf);
        Assert.Contains("Method added: ReplaceLineEndings()", mdf);

        // TypeName should NOT appear as a column or field
        Assert.DoesNotContain("| TypeName", mdf);
    }

    [Fact]
    public void Serialize_GroupBy_MultipleProperties_RendersTablePerGroup()
    {
        var report = new MetricReport
        {
            Title = "Perf Results",
            Metrics =
            [
                new("CPU", "Allocation", "12 MB"),
                new("CPU", "Duration", "3.2s"),
                new("Memory", "Peak", "256 MB"),
                new("Memory", "Average", "128 MB"),
            ]
        };

        var mdf = MarkoutSerializer.Serialize(report, GroupByTestContext.Default);

        // Group subheadings
        Assert.Contains("### CPU", mdf);
        Assert.Contains("### Memory", mdf);

        // Table headers (Name and Value, not Category)
        Assert.Contains("| Name", mdf);
        Assert.Contains("| Value", mdf);
        Assert.DoesNotContain("| Category", mdf);

        // Table data
        Assert.Contains("Allocation", mdf);
        Assert.Contains("12 MB", mdf);
        Assert.Contains("Peak", mdf);
        Assert.Contains("256 MB", mdf);
    }

    [Fact]
    public void Serialize_GroupBy_EmptyList_SkipsSection()
    {
        var report = new DiffReport
        {
            Title = "No Changes",
            Versions = "1.0 → 1.0",
            BreakingChanges = []
        };

        var mdf = MarkoutSerializer.Serialize(report, GroupByTestContext.Default);

        Assert.DoesNotContain("Breaking Changes", mdf);
    }

    [Fact]
    public void Serialize_GroupBy_NullList_SkipsSection()
    {
        var report = new DiffReport
        {
            Title = "No Changes",
            Versions = "1.0 → 1.0",
        };

        var mdf = MarkoutSerializer.Serialize(report, GroupByTestContext.Default);

        Assert.DoesNotContain("Breaking Changes", mdf);
        Assert.DoesNotContain("Additive Changes", mdf);
    }
}

public class NestedCalloutSectionTests
{
    [Fact]
    public void Serialize_CalloutPlusSection_RendersSubsectionsWithCalloutAndTable()
    {
        var report = new VerificationReport
        {
            Title = "Verification",
            Generated = "2026-02-22",
            Families =
            [
                new()
                {
                    Name = "Linux",
                    Distros =
                    [
                        new()
                        {
                            Name = "Ubuntu",
                            Alert = new(CalloutSeverity.Warning, "EOL but still supported"),
                            Issues = [new("22.04", "2027-04-01")]
                        }
                    ]
                }
            ]
        };

        var md = MarkoutSerializer.Serialize(report, NestedCalloutTestContext.Default);

        // Heading hierarchy: families → distros as subsections
        Assert.Contains("## Families", md);
        Assert.Contains("### Linux", md);
        Assert.Contains("#### Distros", md);
        Assert.Contains("##### Ubuntu", md);

        // Callout renders inside the distro subsection
        Assert.Contains("> [!WARNING]", md);
        Assert.Contains("> EOL but still supported", md);

        // Table renders inside the distro subsection
        Assert.Contains("| Version | EOL Date |", md);
        Assert.Contains("| 22.04 | 2027-04-01 |", md);

        // Callout should appear before the table
        int calloutPos = md.IndexOf("> [!WARNING]");
        int tablePos = md.IndexOf("| Version |");
        Assert.True(calloutPos < tablePos, "Callout should appear before the table");
    }

    [Fact]
    public void Serialize_EmptyIssues_SkipsSection()
    {
        var report = new VerificationReport
        {
            Title = "Clean",
            Generated = "2026-02-22",
            Families = []
        };

        var md = MarkoutSerializer.Serialize(report, NestedCalloutTestContext.Default);

        Assert.DoesNotContain("## Families", md);
        Assert.Contains("# Clean", md);
        Assert.Contains("| Generated | 2026-02-22 |", md);
    }
}

public class UnwrapTests
{
    [Fact]
    public void Serialize_Unwrap_RendersItemsWithoutParentHeading()
    {
        var report = new UnwrapReport
        {
            Title = "Verification",
            Generated = "2026-02-22",
            Families =
            [
                new()
                {
                    Name = "Linux",
                    Distros =
                    [
                        new()
                        {
                            Name = "Ubuntu",
                            Alert = new(CalloutSeverity.Warning, "EOL but still supported"),
                            Issues = [new("22.04", "2027-04-01")]
                        }
                    ]
                }
            ]
        };

        var md = MarkoutSerializer.Serialize(report, UnwrapTestContext.Default);

        // No intermediate "Families" or "Distros" headings
        Assert.DoesNotContain("Families", md);
        Assert.DoesNotContain("Distros", md);

        // Heading hierarchy: H1 title, H2 family, H3 distro
        Assert.Contains("# Verification", md);
        Assert.Contains("## Linux", md);
        Assert.Contains("### Ubuntu", md);

        // Callout and table render inside distro
        Assert.Contains("> [!WARNING]", md);
        Assert.Contains("| Version | EOL Date |", md);
        Assert.Contains("| 22.04 | 2027-04-01 |", md);
    }

    [Fact]
    public void Serialize_Unwrap_MultipleItemsAtSameLevel()
    {
        var report = new UnwrapReport
        {
            Title = "Report",
            Generated = "2026-02-22",
            Families =
            [
                new() { Name = "Linux", Distros = [] },
                new() { Name = "macOS", Distros = [] }
            ]
        };

        var md = MarkoutSerializer.Serialize(report, UnwrapTestContext.Default);

        // Both families at H2, no wrapper heading
        Assert.Contains("## Linux", md);
        Assert.Contains("## macOS", md);
        Assert.DoesNotContain("Families", md);
    }

    [Fact]
    public void Serialize_Unwrap_EmptyList_SkipsContent()
    {
        var report = new UnwrapReport
        {
            Title = "Clean",
            Generated = "2026-02-22",
            Families = []
        };

        var md = MarkoutSerializer.Serialize(report, UnwrapTestContext.Default);

        Assert.Contains("# Clean", md);
        Assert.DoesNotContain("##", md);
    }

    [Fact]
    public void NestedObject_RespectsNamingPolicy_PascalCaseWords()
    {
        var report = new LibraryReport
        {
            FileName = "System.Text.Json.dll",
            Info = new LibraryInfoSection
            {
                Name = "System.Text.Json",
                Version = "11.0.0",
                InformationalVersion = "11.0.0+abc123",
                AssemblyVersion = "11.0.0.0",
                TargetFramework = ".NETCoreApp,Version=v11.0"
            }
        };

        var md = MarkoutSerializer.Serialize(report, NestedTestContext.Default);

        // NamingPolicy should split PascalCase into words
        Assert.Contains("Informational Version", md);
        Assert.Contains("Assembly Version", md);
        Assert.Contains("Target Framework", md);
        // Should NOT contain unsplit names
        Assert.DoesNotContain("InformationalVersion", md);
        Assert.DoesNotContain("AssemblyVersion", md);
        Assert.DoesNotContain("TargetFramework", md);
    }

    [Fact]
    public void NestedObject_RespectsFieldLayout_LineBreaks()
    {
        var report = new LibraryReport
        {
            FileName = "System.Text.Json.dll",
            Info = new LibraryInfoSection
            {
                Name = "System.Text.Json",
                Version = "11.0.0",
                AssemblyVersion = "11.0.0.0",
            }
        };

        var md = MarkoutSerializer.Serialize(report, NestedTestContext.Default);

        // Table layout renders fields as markdown table
        Assert.Contains("| Name | System.Text.Json |", md);
        Assert.Contains("| Version | 11.0.0 |", md);
        Assert.Contains("| Assembly Version | 11.0.0.0 |", md);
    }

    [Fact]
    public void NestedObject_SkipNull_HidesNullFields()
    {
        var report = new LibraryReport
        {
            FileName = "Test.dll",
            Info = new LibraryInfoSection
            {
                Name = "Test",
                Version = "1.0.0",
                // InformationalVersion, AssemblyVersion, TargetFramework are null
            }
        };

        var md = MarkoutSerializer.Serialize(report, NestedTestContext.Default);

        Assert.Contains("| Name | Test |", md);
        Assert.Contains("| Version | 1.0.0 |", md);
        // Null fields should be skipped
        Assert.DoesNotContain("Informational Version", md);
        Assert.DoesNotContain("Assembly Version", md);
        Assert.DoesNotContain("Target Framework", md);
    }

    [Fact]
    public void ComplexArray_RespectsNamingPolicy_PascalCaseWords()
    {
        var report = new AttributeReport
        {
            Title = "Attributes",
            Attributes = [
                new AttributeRow { Name = "CLSCompliant", TargetKind = "Assembly", AttributeValue = "true" },
                new AttributeRow { Name = "Extension", TargetKind = "Assembly", AttributeValue = "" },
            ]
        };

        var md = MarkoutSerializer.Serialize(report, NestedTestContext.Default);

        // Table column headers should have PascalCase split
        Assert.Contains("Target Kind", md);
        Assert.Contains("Attribute Value", md);
        // Should NOT contain unsplit names
        Assert.DoesNotContain("TargetKind", md);
        Assert.DoesNotContain("AttributeValue", md);
    }

    [Fact]
    public void NestedObject_BoolWithSkipNull_DoesNotCorruptOutput()
    {
        var report = new LibraryReport
        {
            FileName = "Test.dll",
            Info = new LibraryInfoSection
            {
                Name = "Test",
                Version = "1.0.0",
                Deterministic = true,
                Reproducible = false,
            }
        };

        var md = MarkoutSerializer.Serialize(report, NestedTestContext.Default);

        // Bool fields should render with custom format despite class-level [MarkoutSkipNull]
        Assert.Contains("| Deterministic | Yes |", md);
        Assert.Contains("| Reproducible | No |", md);
        // Null strings should still be skipped
        Assert.DoesNotContain("Informational Version", md);
        Assert.DoesNotContain("Assembly Version", md);
    }
}
