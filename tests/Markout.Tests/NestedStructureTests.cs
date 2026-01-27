using Markout;
using Xunit;

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
        public List<string>? Tags { get; set; }
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
        
        public List<string>? Features { get; set; }
    }

    [MarkoutSerializable]
    public class Service
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public bool Enabled { get; set; }
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
        Assert.Contains("Age: 30", mdf);
        
        // Should have section for nested object
        Assert.Contains("## Contact Info", mdf);
        
        // Should have nested object fields
        Assert.Contains("Email: john@example.com", mdf);
        Assert.Contains("Phone: 555-1234", mdf);
        Assert.Contains("City: Seattle", mdf);
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
        Assert.Contains("Age: 25", mdf);
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
        Assert.Contains("Version: 1.0.0", mdf);
        
        // Nested object section
        Assert.Contains("## Team Info", mdf);
        Assert.Contains("Lead: Alice", mdf);
        Assert.Contains("Size: 3", mdf);
        
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
        Assert.Contains("Version: 13.0.3", mdf);
        
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
}

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
        Assert.Contains("Architecture: AnyCPU", mdf);
        Assert.Contains("Signed: yes", mdf);
        
        // First nested section
        Assert.Contains("## Assembly Info", mdf);
        Assert.Contains("Assembly Version: 6.0.0.0", mdf);
        Assert.Contains("File Version: 6.0.21.52210", mdf);
        Assert.Contains("Target Framework: net6.0", mdf);
        
        // Second nested section
        Assert.Contains("## API Surface", mdf);
        Assert.Contains("Public Types: 144", mdf);
        Assert.Contains("Public Methods: 892", mdf);
    }
}
