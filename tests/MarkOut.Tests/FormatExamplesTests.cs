using MarkOut;
using Xunit;
using Xunit.Abstractions;

namespace MarkOut.Tests;

/// <summary>
/// Tests that output actual MDF examples to demonstrate the format and identify nesting limitations.
/// These tests write to xUnit output so you can see what the actual format looks like.
/// </summary>
public class FormatExamplesTests
{
    private readonly ITestOutputHelper _output;

    public FormatExamplesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Example_SimpleNestedObject_ShowsFormat()
    {
        var person = new Person
        {
            Name = "Alice Johnson",
            Age = 30,
            Contact = new ContactInfo
            {
                Email = "alice@example.com",
                Phone = "555-1234",
                City = "Seattle"
            }
        };

        var mdf = MarkOutSerializer.Serialize(person, NestedTestContext.Default);
        
        _output.WriteLine("=== Simple Nested Object ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }

    [Fact]
    public void Example_ListOfObjects_ShowsTableFormat()
    {
        var team = new Team
        {
            Name = "Platform Engineering",
            Tags = new List<string> { "infrastructure", "kubernetes", "terraform" },
            Members = new List<Member>
            {
                new Member { Name = "Alice", Role = "Principal Engineer", Active = true },
                new Member { Name = "Bob", Role = "Senior Engineer", Active = true },
                new Member { Name = "Charlie", Role = "Engineer", Active = true },
                new Member { Name = "Diana", Role = "Engineer", Active = false }
            }
        };

        var mdf = MarkOutSerializer.Serialize(team, NestedTestContext.Default);
        
        _output.WriteLine("=== List of Objects (Table Format) ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }

    [Fact]
    public void Example_NestedObjectWithList_ShowsLimitation()
    {
        var project = new Project
        {
            Name = "Cloud Migration",
            Version = "2.0.0",
            Team = new TeamInfo
            {
                Lead = "Alice Johnson",
                Size = 5,
                Contributors = new List<Contributor>
                {
                    new Contributor { Name = "Bob Smith", Commits = 127 },
                    new Contributor { Name = "Charlie Brown", Commits = 98 },
                    new Contributor { Name = "Diana Prince", Commits = 85 }
                }
            }
        };

        var mdf = MarkOutSerializer.Serialize(project, NestedTestContext.Default);
        
        _output.WriteLine("=== Nested Object with List (2 Levels) ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("NOTE: The nested list (Contributors) is rendered as a table.");
        _output.WriteLine("This works because it's at the second level of nesting.");
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }

    [Fact]
    public void Example_ListOfObjectsWithLists_ShowsProblem()
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
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" },
                        new Dependency { Name = "Microsoft.CSharp", Version = "4.7.0" }
                    }
                },
                new DependencyGroup
                {
                    TargetFramework = "net8.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "Microsoft.CSharp", Version = "4.7.0" }
                    }
                },
                new DependencyGroup
                {
                    TargetFramework = "netstandard2.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" },
                        new Dependency { Name = "System.Runtime", Version = "4.3.1" },
                        new Dependency { Name = "Microsoft.CSharp", Version = "4.7.0" }
                    }
                }
            }
        };

        var mdf = MarkOutSerializer.Serialize(package, NestedTestContext.Default);
        
        _output.WriteLine("=== List of Objects, Each with Nested List ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("PROBLEM: DependencyGroups is a list where each item has a Packages list.");
        _output.WriteLine("The outer list becomes a table, but the nested Packages lists are LOST.");
        _output.WriteLine("Tables cannot contain nested lists in Markdown.");
        _output.WriteLine("");
        _output.WriteLine("This is a fundamental limitation of the format when mapping to Markdown tables.");
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }

    [Fact]
    public void Example_BuildResult_RealWorldUse()
    {
        var result = new BuildResult
        {
            SolutionName = "MyApp.sln",
            Configuration = "Release",
            Platform = "Any CPU",
            Succeeded = true,
            Duration = "00:02:45.123",
            TotalProjects = 8,
            SucceededProjects = 8,
            FailedProjects = 0,
            Projects = new List<ProjectBuild>
            {
                new ProjectBuild
                {
                    Name = "MyApp.Core",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 12500,
                    Warnings = 0,
                    Errors = 0,
                    OutputPath = "bin/Release/net8.0/MyApp.Core.dll"
                },
                new ProjectBuild
                {
                    Name = "MyApp.Data",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 18200,
                    Warnings = 2,
                    Errors = 0,
                    OutputPath = "bin/Release/net8.0/MyApp.Data.dll"
                },
                new ProjectBuild
                {
                    Name = "MyApp.Api",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 23400,
                    Warnings = 0,
                    Errors = 0,
                    OutputPath = "bin/Release/net8.0/MyApp.Api.dll"
                },
                new ProjectBuild
                {
                    Name = "MyApp.Web",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 34500,
                    Warnings = 1,
                    Errors = 0,
                    OutputPath = "bin/Release/net8.0/MyApp.Web.dll"
                },
                new ProjectBuild
                {
                    Name = "MyApp.Tests",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 15600,
                    Warnings = 0,
                    Errors = 0,
                    OutputPath = "bin/Release/net8.0/MyApp.Tests.dll"
                }
            },
            Summary = new BuildSummary
            {
                TotalWarnings = 3,
                TotalErrors = 0,
                BuildTime = "165.2s"
            }
        };

        var mdf = MarkOutSerializer.Serialize(result, BuildResultsContext.Default);
        
        _output.WriteLine("=== Build Result (Real-World Pattern) ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("This is a typical build system output pattern:");
        _output.WriteLine("- Top-level metadata (solution, config, status)");
        _output.WriteLine("- List of projects as a table");
        _output.WriteLine("- Summary section with aggregated data");
        _output.WriteLine("");
        _output.WriteLine("This pattern works well because:");
        _output.WriteLine("1. Projects list is 1 level deep (becomes a table)");
        _output.WriteLine("2. Summary is a nested object with scalars (becomes a section)");
        _output.WriteLine("3. No lists within table rows");
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }

    [Fact]
    public void Example_DeepNesting_ShowsLimitation()
    {
        var org = new Organization
        {
            Name = "TechCorp Industries",
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
                            MemberCount = 8,
                            Projects = new List<string> { "API Gateway", "Auth Service", "Data Pipeline" }
                        },
                        new Team2
                        {
                            Name = "Frontend",
                            MemberCount = 5,
                            Projects = new List<string> { "Web App", "Mobile App", "Admin Portal" }
                        },
                        new Team2
                        {
                            Name = "Platform",
                            MemberCount = 6,
                            Projects = new List<string> { "K8s Operators", "CI/CD", "Monitoring" }
                        }
                    }
                },
                new Department
                {
                    Name = "Product",
                    Teams = new List<Team2>
                    {
                        new Team2
                        {
                            Name = "Product Management",
                            MemberCount = 4,
                            Projects = new List<string> { "Roadmap", "Strategy" }
                        }
                    }
                }
            }
        };

        var mdf = MarkOutSerializer.Serialize(org, NestedTestContext.Default);
        
        _output.WriteLine("=== Deep Nesting (3 Levels) ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("PROBLEM: Three levels of nesting:");
        _output.WriteLine("1. Organization (root)");
        _output.WriteLine("2. Departments (list → table)");
        _output.WriteLine("3. Teams within Departments (nested list → LOST)");
        _output.WriteLine("");
        _output.WriteLine("The Departments table shows, but Teams within each Department are lost.");
        _output.WriteLine("This is because you can't nest tables or lists within table cells.");
        _output.WriteLine("");
        _output.WriteLine("WORKAROUNDS:");
        _output.WriteLine("1. Flatten the structure (avoid deep nesting)");
        _output.WriteLine("2. Use sections instead of tables for the outer list");
        _output.WriteLine("3. Accept that some data won't be serialized");
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }

    [Fact]
    public void Example_MultipleSectionsPattern_WorksWell()
    {
        var app = new Application
        {
            Name = "E-Commerce Platform",
            Services = new List<Service>
            {
                new Service { Name = "Product API", Url = "https://api.example.com/products", Enabled = true },
                new Service { Name = "Order API", Url = "https://api.example.com/orders", Enabled = true },
                new Service { Name = "Payment API", Url = "https://api.example.com/payments", Enabled = true },
                new Service { Name = "Notification Service", Url = "https://api.example.com/notifications", Enabled = false }
            },
            Dependencies = new List<Dependency>
            {
                new Dependency { Name = "PostgreSQL", Version = "15.2" },
                new Dependency { Name = "Redis", Version = "7.0.8" },
                new Dependency { Name = "RabbitMQ", Version = "3.11.10" },
                new Dependency { Name = "Elasticsearch", Version = "8.6.0" }
            },
            Features = new List<string>
            {
                "Product Catalog",
                "Shopping Cart",
                "Order Processing",
                "Payment Integration",
                "Email Notifications",
                "Search Functionality"
            }
        };

        var mdf = MarkOutSerializer.Serialize(app, NestedTestContext.Default);
        
        _output.WriteLine("=== Multiple Sections at Same Level ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("This pattern works well:");
        _output.WriteLine("- String list (Features) → bullet list");
        _output.WriteLine("- Object list (Services) → table in section");
        _output.WriteLine("- Object list (Dependencies) → table in section");
        _output.WriteLine("");
        _output.WriteLine("All sections are siblings at the same level (H2).");
        _output.WriteLine("No problematic nesting.");
        _output.WriteLine("");

        Assert.NotEmpty(mdf);
    }
}
