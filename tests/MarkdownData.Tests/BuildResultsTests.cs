using MarkdownData;
using Xunit;

namespace MarkdownData.Tests;

/// <summary>
/// Tests that mimic real-world build system output like dotnet build, MSBuild, etc.
/// These tests validate the format's ability to represent hierarchical build results
/// with projects, targets, tasks, warnings, and errors.
/// </summary>

#region Build Results Models

[MdfSerializable(TitleProperty = nameof(SolutionName))]
public class BuildResult
{
    [MdfPropertyName("Solution")]
    public string SolutionName { get; set; } = "";
    
    public string Configuration { get; set; } = "";
    public string Platform { get; set; } = "";
    public bool Succeeded { get; set; }
    
    [MdfPropertyName("Build Duration")]
    public string? Duration { get; set; }
    
    [MdfPropertyName("Total Projects")]
    public int TotalProjects { get; set; }
    
    [MdfPropertyName("Succeeded Projects")]
    public int SucceededProjects { get; set; }
    
    [MdfPropertyName("Failed Projects")]
    public int FailedProjects { get; set; }
    
    [MdfSection(Name = "Projects")]
    public List<ProjectBuild>? Projects { get; set; }
    
    [MdfSection(Name = "Build Summary")]
    public BuildSummary? Summary { get; set; }
}

[MdfSerializable]
public class ProjectBuild
{
    public string Name { get; set; } = "";
    
    [MdfPropertyName("Target Framework")]
    public string? TargetFramework { get; set; }
    
    public bool Succeeded { get; set; }
    
    [MdfPropertyName("Duration (ms)")]
    public int DurationMs { get; set; }
    
    public int Warnings { get; set; }
    public int Errors { get; set; }
    
    [MdfPropertyName("Output Path")]
    public string? OutputPath { get; set; }
}

[MdfSerializable]
public class BuildSummary
{
    [MdfPropertyName("Total Warnings")]
    public int TotalWarnings { get; set; }
    
    [MdfPropertyName("Total Errors")]
    public int TotalErrors { get; set; }
    
    [MdfPropertyName("Build Time")]
    public string? BuildTime { get; set; }
}

// More detailed build with target execution
[MdfSerializable(TitleProperty = nameof(ProjectName))]
public class DetailedBuildResult
{
    [MdfPropertyName("Project")]
    public string ProjectName { get; set; } = "";
    
    [MdfPropertyName("Project File")]
    public string? ProjectFile { get; set; }
    
    [MdfPropertyName("Target Framework")]
    public string? TargetFramework { get; set; }
    
    public string Configuration { get; set; } = "";
    public bool Succeeded { get; set; }
    
    [MdfSection(Name = "Targets")]
    public List<TargetExecution>? Targets { get; set; }
    
    [MdfSection(Name = "Diagnostics")]
    public DiagnosticsInfo? Diagnostics { get; set; }
}

[MdfSerializable]
public class TargetExecution
{
    [MdfPropertyName("Target Name")]
    public string Name { get; set; } = "";
    
    public string Status { get; set; } = "";
    
    [MdfPropertyName("Duration (ms)")]
    public int DurationMs { get; set; }
    
    public bool Skipped { get; set; }
}

[MdfSerializable]
public class DiagnosticsInfo
{
    public int Warnings { get; set; }
    public int Errors { get; set; }
    
    [MdfPropertyName("Warning Codes")]
    public List<string>? WarningCodes { get; set; }
    
    [MdfPropertyName("Error Codes")]
    public List<string>? ErrorCodes { get; set; }
}

// Test execution results
[MdfSerializable(TitleProperty = nameof(TestSuiteName))]
public class TestRunResult
{
    [MdfPropertyName("Test Suite")]
    public string TestSuiteName { get; set; } = "";
    
    public string Framework { get; set; } = "";
    public bool Passed { get; set; }
    
    [MdfPropertyName("Total Tests")]
    public int TotalTests { get; set; }
    
    [MdfPropertyName("Passed Tests")]
    public int PassedTests { get; set; }
    
    [MdfPropertyName("Failed Tests")]
    public int FailedTests { get; set; }
    
    [MdfPropertyName("Skipped Tests")]
    public int SkippedTests { get; set; }
    
    [MdfPropertyName("Total Duration")]
    public string? Duration { get; set; }
    
    [MdfSection(Name = "Test Assemblies")]
    public List<TestAssembly>? Assemblies { get; set; }
    
    [MdfSection(Name = "Failed Tests")]
    public List<FailedTest>? FailedTestsList { get; set; }
}

[MdfSerializable]
public class TestAssembly
{
    public string Name { get; set; } = "";
    public int Tests { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    
    [MdfPropertyName("Duration (s)")]
    public double DurationSeconds { get; set; }
}

[MdfSerializable]
public class FailedTest
{
    [MdfPropertyName("Test Name")]
    public string Name { get; set; } = "";
    
    public string? Class { get; set; }
    public string? Error { get; set; }
    
    [MdfPropertyName("Stack Trace")]
    public string? StackTrace { get; set; }
}

// CI/CD pipeline result
[MdfSerializable(TitleProperty = nameof(PipelineName))]
public class PipelineResult
{
    [MdfPropertyName("Pipeline")]
    public string PipelineName { get; set; } = "";
    
    [MdfPropertyName("Run Number")]
    public int RunNumber { get; set; }
    
    public string Branch { get; set; } = "";
    public string Commit { get; set; } = "";
    public string Status { get; set; } = "";
    
    [MdfPropertyName("Started At")]
    public string? StartedAt { get; set; }
    
    [MdfPropertyName("Completed At")]
    public string? CompletedAt { get; set; }
    
    [MdfPropertyName("Total Duration")]
    public string? Duration { get; set; }
    
    [MdfSection(Name = "Stages")]
    public List<PipelineStage>? Stages { get; set; }
}

[MdfSerializable]
public class PipelineStage
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    
    [MdfPropertyName("Duration (s)")]
    public int DurationSeconds { get; set; }
    
    [MdfPropertyName("Started At")]
    public string? StartedAt { get; set; }
}

#endregion

#region Test Context

[MdfContext(typeof(BuildResult))]
[MdfContext(typeof(DetailedBuildResult))]
[MdfContext(typeof(TestRunResult))]
[MdfContext(typeof(PipelineResult))]
public partial class BuildResultsContext : MdfSerializerContext
{
}

#endregion

public class BuildResultsTests
{
    #region Basic Build Results

    [Fact]
    public void Serialize_SimpleBuildResult_Success()
    {
        var result = new BuildResult
        {
            SolutionName = "MyApp.sln",
            Configuration = "Debug",
            Platform = "Any CPU",
            Succeeded = true,
            Duration = "00:00:12.345",
            TotalProjects = 5,
            SucceededProjects = 5,
            FailedProjects = 0,
            Projects = new List<ProjectBuild>
            {
                new ProjectBuild
                {
                    Name = "MyApp.Core",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 2500,
                    Warnings = 0,
                    Errors = 0,
                    OutputPath = "bin/Debug/net8.0/MyApp.Core.dll"
                },
                new ProjectBuild
                {
                    Name = "MyApp.Api",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 3200,
                    Warnings = 2,
                    Errors = 0,
                    OutputPath = "bin/Debug/net8.0/MyApp.Api.dll"
                },
                new ProjectBuild
                {
                    Name = "MyApp.Tests",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 1800,
                    Warnings = 0,
                    Errors = 0,
                    OutputPath = "bin/Debug/net8.0/MyApp.Tests.dll"
                }
            },
            Summary = new BuildSummary
            {
                TotalWarnings = 2,
                TotalErrors = 0,
                BuildTime = "12.345s"
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        // Title and basic fields
        Assert.Contains("# MyApp.sln", mdf);
        Assert.Contains("Configuration: Debug", mdf);
        Assert.Contains("Platform: Any CPU", mdf);
        Assert.Contains("Succeeded: yes", mdf);
        Assert.Contains("Build Duration: 00:00:12.345", mdf);
        Assert.Contains("Total Projects: 5", mdf);
        Assert.Contains("Succeeded Projects: 5", mdf);
        Assert.Contains("Failed Projects: 0", mdf);

        // Projects table
        Assert.Contains("## Projects", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Target Framework |", mdf);
        Assert.Contains("| Succeeded |", mdf);
        Assert.Contains("| Duration (ms) |", mdf);
        Assert.Contains("| Warnings |", mdf);
        Assert.Contains("| Errors |", mdf);
        Assert.Contains("| MyApp.Core |", mdf);
        Assert.Contains("| MyApp.Api |", mdf);
        Assert.Contains("| MyApp.Tests |", mdf);

        // Summary section
        Assert.Contains("## Build Summary", mdf);
        Assert.Contains("Total Warnings: 2", mdf);
        Assert.Contains("Total Errors: 0", mdf);
        Assert.Contains("Build Time: 12.345s", mdf);
    }

    [Fact]
    public void Serialize_BuildResultWithFailures_ShowsErrors()
    {
        var result = new BuildResult
        {
            SolutionName = "FailingApp.sln",
            Configuration = "Release",
            Platform = "x64",
            Succeeded = false,
            Duration = "00:00:08.123",
            TotalProjects = 3,
            SucceededProjects = 2,
            FailedProjects = 1,
            Projects = new List<ProjectBuild>
            {
                new ProjectBuild
                {
                    Name = "App.Core",
                    TargetFramework = "net8.0",
                    Succeeded = true,
                    DurationMs = 1500,
                    Warnings = 0,
                    Errors = 0,
                    OutputPath = "bin/Release/net8.0/App.Core.dll"
                },
                new ProjectBuild
                {
                    Name = "App.Api",
                    TargetFramework = "net8.0",
                    Succeeded = false,
                    DurationMs = 800,
                    Warnings = 3,
                    Errors = 5,
                    OutputPath = null
                }
            },
            Summary = new BuildSummary
            {
                TotalWarnings = 3,
                TotalErrors = 5,
                BuildTime = "8.123s"
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        Assert.Contains("# FailingApp.sln", mdf);
        Assert.Contains("Succeeded: no", mdf);
        Assert.Contains("Failed Projects: 1", mdf);
        Assert.Contains("| App.Api |", mdf);
        Assert.Contains("Total Errors: 5", mdf);
    }

    #endregion

    #region Detailed Build with Targets

    [Fact]
    public void Serialize_DetailedBuildWithTargets_ShowsTargetExecution()
    {
        var result = new DetailedBuildResult
        {
            ProjectName = "MyLibrary",
            ProjectFile = "src/MyLibrary/MyLibrary.csproj",
            TargetFramework = "netstandard2.0",
            Configuration = "Release",
            Succeeded = true,
            Targets = new List<TargetExecution>
            {
                new TargetExecution { Name = "Restore", Status = "Succeeded", DurationMs = 1234, Skipped = false },
                new TargetExecution { Name = "CoreCompile", Status = "Succeeded", DurationMs = 2567, Skipped = false },
                new TargetExecution { Name = "GenerateNuspec", Status = "Succeeded", DurationMs = 45, Skipped = false },
                new TargetExecution { Name = "Pack", Status = "Succeeded", DurationMs = 123, Skipped = false }
            },
            Diagnostics = new DiagnosticsInfo
            {
                Warnings = 0,
                Errors = 0,
                WarningCodes = new List<string>(),
                ErrorCodes = new List<string>()
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        // Title
        Assert.Contains("# MyLibrary", mdf);
        Assert.Contains("Project File: src/MyLibrary/MyLibrary.csproj", mdf);
        Assert.Contains("Target Framework: netstandard2.0", mdf);
        Assert.Contains("Configuration: Release", mdf);
        Assert.Contains("Succeeded: yes", mdf);

        // Targets table
        Assert.Contains("## Targets", mdf);
        Assert.Contains("| Target Name |", mdf);
        Assert.Contains("| Status |", mdf);
        Assert.Contains("| Duration (ms) |", mdf);
        Assert.Contains("| Skipped |", mdf);
        Assert.Contains("| Restore |", mdf);
        Assert.Contains("| CoreCompile |", mdf);
        Assert.Contains("| GenerateNuspec |", mdf);
        Assert.Contains("| Pack |", mdf);

        // Diagnostics section
        Assert.Contains("## Diagnostics", mdf);
        Assert.Contains("Warnings: 0", mdf);
        Assert.Contains("Errors: 0", mdf);
    }

    [Fact]
    public void Serialize_BuildWithWarnings_ShowsDiagnosticCodes()
    {
        var result = new DetailedBuildResult
        {
            ProjectName = "LegacyApp",
            Configuration = "Debug",
            Succeeded = true,
            Diagnostics = new DiagnosticsInfo
            {
                Warnings = 5,
                Errors = 0,
                WarningCodes = new List<string> { "CS0618", "CS8600", "CS8603", "CS8604", "CS8625" },
                ErrorCodes = new List<string>()
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        Assert.Contains("# LegacyApp", mdf);
        Assert.Contains("## Diagnostics", mdf);
        Assert.Contains("Warnings: 5", mdf);
        Assert.Contains("Warning Codes:", mdf);
        Assert.Contains("- CS0618", mdf);
        Assert.Contains("- CS8600", mdf);
        Assert.Contains("- CS8625", mdf);
    }

    #endregion

    #region Test Run Results

    [Fact]
    public void Serialize_TestRunResult_AllPassed()
    {
        var result = new TestRunResult
        {
            TestSuiteName = "MyApp Test Suite",
            Framework = "xUnit.net 2.5.3",
            Passed = true,
            TotalTests = 247,
            PassedTests = 247,
            FailedTests = 0,
            SkippedTests = 3,
            Duration = "00:01:23.456",
            Assemblies = new List<TestAssembly>
            {
                new TestAssembly
                {
                    Name = "MyApp.Core.Tests.dll",
                    Tests = 120,
                    Passed = 120,
                    Failed = 0,
                    Skipped = 2,
                    DurationSeconds = 45.2
                },
                new TestAssembly
                {
                    Name = "MyApp.Api.Tests.dll",
                    Tests = 127,
                    Passed = 127,
                    Failed = 0,
                    Skipped = 1,
                    DurationSeconds = 38.3
                }
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        // Title
        Assert.Contains("# MyApp Test Suite", mdf);
        Assert.Contains("Framework: xUnit.net 2.5.3", mdf);
        Assert.Contains("Passed: yes", mdf);
        Assert.Contains("Total Tests: 247", mdf);
        Assert.Contains("Passed Tests: 247", mdf);
        Assert.Contains("Failed Tests: 0", mdf);
        Assert.Contains("Skipped Tests: 3", mdf);
        Assert.Contains("Total Duration: 00:01:23.456", mdf);

        // Test assemblies table
        Assert.Contains("## Test Assemblies", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Tests |", mdf);
        Assert.Contains("| Passed |", mdf);
        Assert.Contains("| Failed |", mdf);
        Assert.Contains("| Skipped |", mdf);
        Assert.Contains("| Duration (s) |", mdf);
        Assert.Contains("| MyApp.Core.Tests.dll |", mdf);
        Assert.Contains("| MyApp.Api.Tests.dll |", mdf);

        // No failed tests section
        Assert.DoesNotContain("## Failed Tests", mdf);
    }

    [Fact]
    public void Serialize_TestRunResult_WithFailures()
    {
        var result = new TestRunResult
        {
            TestSuiteName = "Integration Tests",
            Framework = "NUnit 3.14",
            Passed = false,
            TotalTests = 89,
            PassedTests = 85,
            FailedTests = 4,
            SkippedTests = 0,
            Duration = "00:02:15.789",
            Assemblies = new List<TestAssembly>
            {
                new TestAssembly
                {
                    Name = "IntegrationTests.dll",
                    Tests = 89,
                    Passed = 85,
                    Failed = 4,
                    Skipped = 0,
                    DurationSeconds = 135.8
                }
            },
            FailedTestsList = new List<FailedTest>
            {
                new FailedTest
                {
                    Name = "DatabaseConnectionTest",
                    Class = "IntegrationTests.DatabaseTests",
                    Error = "Connection timeout"
                },
                new FailedTest
                {
                    Name = "ApiEndpointTest",
                    Class = "IntegrationTests.ApiTests",
                    Error = "HTTP 500 Internal Server Error"
                },
                new FailedTest
                {
                    Name = "CacheTest",
                    Class = "IntegrationTests.CacheTests",
                    Error = "Redis connection failed"
                },
                new FailedTest
                {
                    Name = "QueueTest",
                    Class = "IntegrationTests.QueueTests",
                    Error = "Message queue unavailable"
                }
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        Assert.Contains("# Integration Tests", mdf);
        Assert.Contains("Passed: no", mdf);
        Assert.Contains("Failed Tests: 4", mdf);

        // Failed tests table
        Assert.Contains("## Failed Tests", mdf);
        Assert.Contains("| Test Name |", mdf);
        Assert.Contains("| Class |", mdf);
        Assert.Contains("| Error |", mdf);
        Assert.Contains("| DatabaseConnectionTest |", mdf);
        Assert.Contains("| ApiEndpointTest |", mdf);
        Assert.Contains("| Connection timeout |", mdf);
        Assert.Contains("| HTTP 500 Internal Server Error |", mdf);
    }

    #endregion

    #region CI/CD Pipeline Results

    [Fact]
    public void Serialize_PipelineResult_SuccessfulRun()
    {
        var result = new PipelineResult
        {
            PipelineName = "Build and Deploy",
            RunNumber = 1234,
            Branch = "main",
            Commit = "a1b2c3d4",
            Status = "Succeeded",
            StartedAt = "2024-01-15T10:30:00Z",
            CompletedAt = "2024-01-15T10:45:32Z",
            Duration = "00:15:32",
            Stages = new List<PipelineStage>
            {
                new PipelineStage { Name = "Build", Status = "Succeeded", DurationSeconds = 245, StartedAt = "10:30:00" },
                new PipelineStage { Name = "Test", Status = "Succeeded", DurationSeconds = 387, StartedAt = "10:34:05" },
                new PipelineStage { Name = "Package", Status = "Succeeded", DurationSeconds = 89, StartedAt = "10:40:32" },
                new PipelineStage { Name = "Deploy", Status = "Succeeded", DurationSeconds = 211, StartedAt = "10:42:01" }
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        // Title
        Assert.Contains("# Build and Deploy", mdf);
        Assert.Contains("Run Number: 1234", mdf);
        Assert.Contains("Branch: main", mdf);
        Assert.Contains("Commit: a1b2c3d4", mdf);
        Assert.Contains("Status: Succeeded", mdf);
        Assert.Contains("Total Duration: 00:15:32", mdf);

        // Stages table
        Assert.Contains("## Stages", mdf);
        Assert.Contains("| Name |", mdf);
        Assert.Contains("| Status |", mdf);
        Assert.Contains("| Duration (s) |", mdf);
        Assert.Contains("| Started At |", mdf);
        Assert.Contains("| Build |", mdf);
        Assert.Contains("| Test |", mdf);
        Assert.Contains("| Package |", mdf);
        Assert.Contains("| Deploy |", mdf);
    }

    [Fact]
    public void Serialize_PipelineResult_FailedStage()
    {
        var result = new PipelineResult
        {
            PipelineName = "Release Pipeline",
            RunNumber = 456,
            Branch = "release/v2.0",
            Commit = "xyz789",
            Status = "Failed",
            Duration = "00:08:45",
            Stages = new List<PipelineStage>
            {
                new PipelineStage { Name = "Build", Status = "Succeeded", DurationSeconds = 198, StartedAt = "14:00:00" },
                new PipelineStage { Name = "Test", Status = "Failed", DurationSeconds = 327, StartedAt = "14:03:18" },
                new PipelineStage { Name = "Deploy", Status = "Skipped", DurationSeconds = 0, StartedAt = "14:08:45" }
            }
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        Assert.Contains("# Release Pipeline", mdf);
        Assert.Contains("Status: Failed", mdf);
        Assert.Contains("| Test |", mdf);
        Assert.Contains("| Failed |", mdf);
        Assert.Contains("| Skipped |", mdf);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Serialize_EmptyBuildResult_NoProjects()
    {
        var result = new BuildResult
        {
            SolutionName = "Empty.sln",
            Configuration = "Debug",
            Platform = "Any CPU",
            Succeeded = true,
            TotalProjects = 0,
            SucceededProjects = 0,
            FailedProjects = 0,
            Projects = new List<ProjectBuild>()
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        Assert.Contains("# Empty.sln", mdf);
        Assert.Contains("Total Projects: 0", mdf);
        // Empty projects list should not create section
        Assert.DoesNotContain("## Projects", mdf);
    }

    [Fact]
    public void Serialize_BuildResult_NoSummary()
    {
        var result = new BuildResult
        {
            SolutionName = "Simple.sln",
            Configuration = "Debug",
            Platform = "Any CPU",
            Succeeded = true,
            TotalProjects = 1,
            SucceededProjects = 1,
            FailedProjects = 0,
            Summary = null
        };

        var mdf = MdfSerializer.Serialize(result, BuildResultsContext.Default);

        Assert.Contains("# Simple.sln", mdf);
        // Null summary should not create section
        Assert.DoesNotContain("## Build Summary", mdf);
    }

    #endregion
}
