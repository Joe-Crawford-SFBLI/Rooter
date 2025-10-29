using System.Text.Json;
using Rooter.Application.Interfaces;
using Rooter.Domain.Entities;
using Rooter.Infrastructure.Entities;
using Rooter.Infrastructure.Mappers;

namespace Rooter.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of project assets parsing.
/// Handles the actual file I/O and JSON deserialization.
/// </summary>
public class ProjectAssetsParser : IProjectAssetsParser
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ProjectAssetsParser()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Parses a project.assets.json file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the project.assets.json file</param>
    /// <returns>The parsed project assets data, or null if parsing fails</returns>
    public async Task<ProjectAssets?> ParseAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var jsonContent = await File.ReadAllTextAsync(filePath);
            return await ParseFromJsonAsync(jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to parse {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses project.assets.json content from a JSON string.
    /// </summary>
    /// <param name="jsonContent">The JSON content of the project.assets.json file</param>
    /// <returns>The parsed project assets data, or null if parsing fails</returns>
    public async Task<ProjectAssets?> ParseFromJsonAsync(string jsonContent)
    {
        try
        {
            var entity = await Task.Run(() =>
                JsonSerializer.Deserialize<ProjectAssetsEntity>(jsonContent, _jsonOptions));

            if (entity == null)
                return null;

            return ProjectAssetsMapper.ToDomain(entity);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON parsing failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Unexpected error during parsing: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts package references from parsed project assets.
    /// </summary>
    /// <param name="projectAssets">The parsed project assets</param>
    /// <param name="targetFramework">Optional target framework to filter by</param>
    /// <returns>A list of package references</returns>
    public List<PackageReference> ExtractPackages(
        ProjectAssets projectAssets,
        string? targetFramework = null)
    {
        var packages = new List<PackageReference>();
        var processedPackages = new HashSet<string>();

        // Use the specified target framework or the first available one
        var targetToUse = _GetTargetFramework(projectAssets, targetFramework);
        if (targetToUse == null)
            return packages;

        // Extract direct dependencies
        foreach (var dependency in targetToUse.Dependencies.Values)
        {
            if (!processedPackages.Contains(dependency.Id))
            {
                packages.Add(_BuildPackageWithDependencies(
                    dependency,
                    projectAssets,
                    processedPackages));
            }
        }

        return packages;
    }

    private TargetFramework? _GetTargetFramework(
        ProjectAssets projectAssets,
        string? targetFramework)
    {
        if (!string.IsNullOrEmpty(targetFramework))
        {
            var exactMatch = projectAssets.Targets
                .FirstOrDefault(t => t.Key.Contains(targetFramework));

            if (exactMatch.Key != null)
                return exactMatch.Value;
        }

        // Return the first target framework if no specific one is requested
        return projectAssets.Targets.Values.FirstOrDefault();
    }

    private PackageReference _BuildPackageWithDependencies(
        PackageReference package,
        ProjectAssets projectAssets,
        HashSet<string> processedPackages,
        int maxDepth = 10,
        int currentDepth = 0)
    {
        if (currentDepth >= maxDepth || processedPackages.Contains(package.Id))
        {
            return new PackageReference
            {
                Name = package.Name,
                Version = package.Version,
                Type = package.Type
            };
        }

        processedPackages.Add(package.Id);

        var dependencies = new List<PackageReference>();

        // Find the library entry for this package
        var libraryKey = projectAssets.Libraries.Keys
            .FirstOrDefault(k => k.StartsWith($"{package.Name}/"));

        if (libraryKey != null && projectAssets.Libraries.TryGetValue(libraryKey, out var library))
        {
            // Get dependencies from the target framework
            foreach (var target in projectAssets.Targets.Values)
            {
                foreach (var targetDep in target.Dependencies.Values)
                {
                    if (targetDep.Name == package.Name)
                    {
                        foreach (var dep in targetDep.Dependencies)
                        {
                            if (!processedPackages.Contains(dep.Id))
                            {
                                dependencies.Add(_BuildPackageWithDependencies(
                                    dep,
                                    projectAssets,
                                    processedPackages,
                                    maxDepth,
                                    currentDepth + 1));
                            }
                        }
                        break;
                    }
                }
                break; // Use first target framework for dependencies
            }
        }

        return new PackageReference
        {
            Name = package.Name,
            Version = package.Version,
            Type = package.Type,
            Dependencies = dependencies
        };
    }
}