using Markout;

namespace Markout.Tests;

/// <summary>
/// Tests that output actual Markout examples to demonstrate the format and identify nesting limitations.
/// These tests write to xUnit output so you can see what the actual format looks like.
/// </summary>
public class FormatExamplesTests
{
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

        var mdf = MarkoutSerializer.Serialize(person, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== Simple Nested Object ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(team, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== List of Objects (Table Format) ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(project, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== Nested Object with List (2 Levels) ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("NOTE: The nested list (Contributors) is rendered as a table.");
        TestContext.Current.TestOutputHelper!.WriteLine("This works because it's at the second level of nesting.");
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(package, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== List of Objects, Each with Nested List ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM: DependencyGroups is a list where each item has a Packages list.");
        TestContext.Current.TestOutputHelper!.WriteLine("The outer list becomes a table, but the nested Packages lists are LOST.");
        TestContext.Current.TestOutputHelper!.WriteLine("Tables cannot contain nested lists in Markdown.");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This is a fundamental limitation of the format when mapping to Markdown tables.");
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(result, BuildResultsContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== Build Result (Real-World Pattern) ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This is a typical build system output pattern:");
        TestContext.Current.TestOutputHelper!.WriteLine("- Top-level metadata (solution, config, status)");
        TestContext.Current.TestOutputHelper!.WriteLine("- List of projects as a table");
        TestContext.Current.TestOutputHelper!.WriteLine("- Summary section with aggregated data");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This pattern works well because:");
        TestContext.Current.TestOutputHelper!.WriteLine("1. Projects list is 1 level deep (becomes a table)");
        TestContext.Current.TestOutputHelper!.WriteLine("2. Summary is a nested object with scalars (becomes a section)");
        TestContext.Current.TestOutputHelper!.WriteLine("3. No lists within table rows");
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(org, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== Deep Nesting (3 Levels) ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM: Three levels of nesting:");
        TestContext.Current.TestOutputHelper!.WriteLine("1. Organization (root)");
        TestContext.Current.TestOutputHelper!.WriteLine("2. Departments (list → table)");
        TestContext.Current.TestOutputHelper!.WriteLine("3. Teams within Departments (nested list → LOST)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("The Departments table shows, but Teams within each Department are lost.");
        TestContext.Current.TestOutputHelper!.WriteLine("This is because you can't nest tables or lists within table cells.");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("WORKAROUNDS:");
        TestContext.Current.TestOutputHelper!.WriteLine("1. Flatten the structure (avoid deep nesting)");
        TestContext.Current.TestOutputHelper!.WriteLine("2. Use sections instead of tables for the outer list");
        TestContext.Current.TestOutputHelper!.WriteLine("3. Accept that some data won't be serialized");
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(app, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== Multiple Sections at Same Level ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This pattern works well:");
        TestContext.Current.TestOutputHelper!.WriteLine("- String list (Features) → bullet list");
        TestContext.Current.TestOutputHelper!.WriteLine("- Object list (Services) → table in section");
        TestContext.Current.TestOutputHelper!.WriteLine("- Object list (Dependencies) → table in section");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("All sections are siblings at the same level (H2).");
        TestContext.Current.TestOutputHelper!.WriteLine("No problematic nesting.");
        TestContext.Current.TestOutputHelper!.WriteLine("");

        Assert.NotEmpty(mdf);
    }
}
