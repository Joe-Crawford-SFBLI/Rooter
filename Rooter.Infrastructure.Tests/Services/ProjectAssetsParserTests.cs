using FluentAssertions;
using Rooter.Infrastructure.Services;
using Xunit;

namespace Rooter.Infrastructure.Tests.Services;

/// <summary>
/// Comprehensive unit tests for the ProjectAssetsParser class.
/// These tests validate that the parser correctly extracts all expected information
/// from project.assets.json files.
/// </summary>
public class ProjectAssetsParserTests
{
    private readonly ProjectAssetsParser _parser;

    public ProjectAssetsParserTests()
    {
        _parser = new ProjectAssetsParser();
    }

    [Fact]
    public async Task ParseAsync_WithValidSimpleFile_ShouldParseAllBasicProperties()
    {
        // Arrange
        var filePath = Path.Combine("TestData", "simple-project.assets.json");

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(3);

        // Validate targets
        result.Targets.Should().NotBeEmpty();
        result.Targets.Should().ContainKey("net8.0");

        var targetFramework = result.Targets["net8.0"];
        targetFramework.Should().NotBeNull();
        targetFramework.Dependencies.Should().NotBeEmpty();
        targetFramework.Dependencies.Should().ContainKey("AutoMapper/12.0.0");
        targetFramework.Dependencies.Should().ContainKey("Microsoft.CSharp/4.7.0");

        // Validate AutoMapper package in targets
        var autoMapperPackage = targetFramework.Dependencies["AutoMapper/12.0.0"];
        autoMapperPackage.Should().NotBeNull();
        autoMapperPackage.Type.Should().Be("package");

        // Validate Microsoft.CSharp dependency
        var csharpPackage = targetFramework.Dependencies["Microsoft.CSharp/4.7.0"];
        csharpPackage.Should().NotBeNull();
        csharpPackage.Type.Should().Be("package");

        // Validate libraries
        result.Libraries.Should().NotBeEmpty();
        result.Libraries.Should().ContainKey("AutoMapper/12.0.0");
        result.Libraries.Should().ContainKey("Microsoft.CSharp/4.7.0");

        var autoMapperLibrary = result.Libraries["AutoMapper/12.0.0"];
        autoMapperLibrary.Should().NotBeNull();
        autoMapperLibrary.Type.Should().Be("package");
        autoMapperLibrary.Sha512.Should().Be("0Rmg0zI5AFu1O/y//o9VGyhxKjhggWpk9mOA1tp0DEVx40c61bs+lnQv+0jUq8XbniF7FKgIVvI1perqiMtLrA==");
        autoMapperLibrary.Path.Should().Be("automapper/12.0.0");
        autoMapperLibrary.Files.Should().NotBeEmpty();
        autoMapperLibrary.Files.Should().Contain("lib/netstandard2.1/AutoMapper.dll");

        // Validate project file dependency groups
        result.ProjectFileDependencyGroups.Should().NotBeEmpty();
        result.ProjectFileDependencyGroups.Should().ContainKey("net8.0");
        var directDependencies = result.ProjectFileDependencyGroups["net8.0"];
        directDependencies.Should().Contain("AutoMapper >= 12.0.0");

        // Validate project info
        result.Project.Should().NotBeNull();
        result.Project!.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task ParseAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var filePath = "non-existent-file.json";

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseFromJsonAsync_WithValidJson_ShouldParseCorrectly()
    {
        // Arrange
        var json = await File.ReadAllTextAsync(Path.Combine("TestData", "simple-project.assets.json"));

        // Act
        var result = await _parser.ParseFromJsonAsync(json);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(3);
        result.Targets.Should().ContainKey("net8.0");
        result.Libraries.Should().ContainKey("AutoMapper/12.0.0");
        result.ProjectFileDependencyGroups.Should().ContainKey("net8.0");
    }

    [Fact]
    public async Task ParseFromJsonAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var invalidJson = "{ invalid json content";

        // Act
        var result = await _parser.ParseFromJsonAsync(invalidJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseFromJsonAsync_WithEmptyJson_ShouldReturnNull()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = await _parser.ParseFromJsonAsync(emptyJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractPackages_FromSimpleProject_ShouldReturnCorrectHierarchy()
    {
        // Arrange
        var projectAssets = await _parser.ParseAsync(Path.Combine("TestData", "simple-project.assets.json"));
        projectAssets.Should().NotBeNull();

        // Act
        var packages = _parser.ExtractPackages(projectAssets!, "net8.0");

        // Assert
        packages.Should().NotBeEmpty();

        // Should have AutoMapper as direct dependency
        var autoMapperPackage = packages.FirstOrDefault(p => p.Name == "AutoMapper");
        autoMapperPackage.Should().NotBeNull();
        autoMapperPackage!.Version.Should().Be("12.0.0");
        autoMapperPackage.Type.Should().Be("package");

        // AutoMapper should have Microsoft.CSharp as a dependency
        autoMapperPackage.Dependencies.Should().NotBeEmpty();
        var csharpDependency = autoMapperPackage.Dependencies.FirstOrDefault(d => d.Name == "Microsoft.CSharp");
        csharpDependency.Should().NotBeNull();
        csharpDependency!.Version.Should().Be("4.7.0");
    }

    [Fact]
    public async Task ExtractPackages_WithNoTargetFramework_ShouldUseFirstAvailableTarget()
    {
        // Arrange
        var projectAssets = await _parser.ParseAsync(Path.Combine("TestData", "simple-project.assets.json"));
        projectAssets.Should().NotBeNull();

        // Act
        var packages = _parser.ExtractPackages(projectAssets!);

        // Assert
        packages.Should().NotBeEmpty();
        packages.Should().Contain(p => p.Name == "AutoMapper");
    }

    [Fact]
    public async Task ExtractPackages_WithNonExistentTargetFramework_ShouldUseFirstAvailableTarget()
    {
        // Arrange
        var projectAssets = await _parser.ParseAsync(Path.Combine("TestData", "simple-project.assets.json"));
        projectAssets.Should().NotBeNull();

        // Act
        var packages = _parser.ExtractPackages(projectAssets!, "net6.0");

        // Assert
        packages.Should().NotBeEmpty();
        packages.Should().Contain(p => p.Name == "AutoMapper");
    }

    [Fact]
    public async Task ParseAsync_WithComplexExampleFile_ShouldParseAllPackages()
    {
        // Arrange
        var exampleFilePath = Path.Combine("..", "..", "..", "..", "Examples", "Example1", "project.assets.json");

        // Act
        var result = await _parser.ParseAsync(exampleFilePath);

        // Assert
        result.Should().NotBeNull("The complex example file should parse successfully");
        result!.Version.Should().Be(3);

        // Check that we have the expected target frameworks
        result.Targets.Should().NotBeEmpty();
        result.Targets.Keys.Should().Contain(key => key.Contains("net8.0"));

        // Check that we have libraries
        result.Libraries.Should().NotBeEmpty();
        result.Libraries.Keys.Should().Contain(key => key.Contains("AutoMapper"));
        result.Libraries.Keys.Should().Contain(key => key.Contains("FluentValidation"));

        // Check project file dependency groups
        result.ProjectFileDependencyGroups.Should().NotBeEmpty();
        var net8Dependencies = result.ProjectFileDependencyGroups.FirstOrDefault(kvp => kvp.Key.Contains("net8.0"));
        net8Dependencies.Should().NotBeNull();
        net8Dependencies.Value.Should().NotBeEmpty();
        net8Dependencies.Value.Should().Contain(dep => dep.Contains("AutoMapper"));
        net8Dependencies.Value.Should().Contain(dep => dep.Contains("FluentValidation"));

        // Check project info
        result.Project.Should().NotBeNull();
        result.Project!.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task ExtractPackages_FromComplexProject_ShouldBuildCorrectDependencyTree()
    {
        // Arrange
        var exampleFilePath = Path.Combine("..", "..", "..", "..", "Examples", "Example1", "project.assets.json");
        var projectAssets = await _parser.ParseAsync(exampleFilePath);
        projectAssets.Should().NotBeNull();

        // Act
        var packages = _parser.ExtractPackages(projectAssets!);

        // Assert
        packages.Should().NotBeEmpty();

        // Should contain direct dependencies
        var directDependencyNames = new[]
        {
            "AutoMapper.Extensions.Microsoft.DependencyInjection",
            "FluentValidation",
            "FluentValidation.AspNetCore",
            "FluentValidation.DependencyInjectionExtensions",
            "Microsoft.AspNetCore.Mvc.NewtonsoftJson",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Relational",
            "Microsoft.Extensions.Http",
            "RequestR",
            "SFBLIFeatureFlags",
            "SFBLIVault",
            "Scrutor"
        };

        foreach (var depName in directDependencyNames)
        {
            packages.Should().Contain(p => p.Name == depName,
                $"Should contain direct dependency {depName}");
        }

        // Validate that dependencies have their own dependencies
        var autoMapperExtensions = packages.FirstOrDefault(p => p.Name == "AutoMapper.Extensions.Microsoft.DependencyInjection");
        autoMapperExtensions.Should().NotBeNull();
        autoMapperExtensions!.Dependencies.Should().NotBeEmpty();
        autoMapperExtensions.Dependencies.Should().Contain(d => d.Name == "AutoMapper");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task ParseFromJsonAsync_WithNullOrWhitespaceJson_ShouldReturnNull(string? json)
    {
        // Act
        var result = await _parser.ParseFromJsonAsync(json ?? "");

        // Assert
        result.Should().BeNull();
    }
}