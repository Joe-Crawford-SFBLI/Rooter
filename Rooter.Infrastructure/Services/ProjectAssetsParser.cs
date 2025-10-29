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

        // Get direct dependencies from projectFileDependencyGroups
        var directDependencies = _GetDirectDependencies(projectAssets, targetFramework);
        if (directDependencies.Count == 0)
            return packages;

        // Use the specified target framework or the first available one
        var targetToUse = _GetTargetFramework(projectAssets, targetFramework);
        if (targetToUse == null)
            return packages;

        // Build package references for direct dependencies
        foreach (var directDep in directDependencies)
        {
            var packageName = _ExtractPackageNameFromDependencyString(directDep);
            var packageId = _FindPackageInTargets(targetToUse, packageName);

            if (!string.IsNullOrEmpty(packageId) && !processedPackages.Contains(packageId))
            {
                packages.Add(_BuildPackageFromTargets(
                    packageId,
                    targetToUse,
                    projectAssets,
                    processedPackages,
                    true));
            }
        }

        return packages;
    }

    private List<string> _GetDirectDependencies(
        ProjectAssets projectAssets,
        string? targetFramework)
    {
        // Get the target framework key
        var frameworkKey = _GetTargetFrameworkKey(projectAssets, targetFramework);
        if (string.IsNullOrEmpty(frameworkKey))
            return new List<string>();

        // Find direct dependencies from projectFileDependencyGroups
        var directDeps = new List<string>();

        // Check if we have projectFileDependencyGroups
        if (projectAssets.ProjectFileDependencyGroups.ContainsKey(frameworkKey))
        {
            directDeps.AddRange(projectAssets.ProjectFileDependencyGroups[frameworkKey]);
        }

        return directDeps;
    }

    private string _ExtractPackageNameFromDependencyString(string dependencyString)
    {
        // Format is usually "PackageName >= Version" or just "PackageName"
        var parts = dependencyString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : dependencyString;
    }

    private string? _FindPackageInTargets(TargetFramework targetFramework, string packageName)
    {
        // Look for a package ID that starts with the package name
        return targetFramework.Dependencies.Keys
            .FirstOrDefault(key => key.StartsWith($"{packageName}/", StringComparison.OrdinalIgnoreCase));
    }

    private PackageReference _BuildPackageFromTargets(
        string packageId,
        TargetFramework targetFramework,
        ProjectAssets projectAssets,
        HashSet<string> processedPackages,
        bool isDirect,
        int maxDepth = 10,
        int currentDepth = 0)
    {
        var parts = packageId.Split('/');
        var packageName = parts.Length > 0 ? parts[0] : packageId;
        var version = parts.Length > 1 ? parts[1] : "1.0.0";

        // If we've reached max depth or are in a circular reference, return basic package info
        if (currentDepth >= maxDepth || processedPackages.Contains(packageId))
        {
            return new PackageReference
            {
                Name = packageName,
                Version = version,
                Type = "package"
            };
        }

        // Mark as processed for this recursion path only
        processedPackages.Add(packageId);

        var dependencies = new List<PackageReference>();

        // Get the package details from the target framework
        if (targetFramework.Dependencies.TryGetValue(packageId, out var packageInfo))
        {
            // Build dependencies recursively
            foreach (var depInfo in packageInfo.Dependencies)
            {
                var depName = depInfo.Name;
                var depPackageId = _FindPackageInTargets(targetFramework, depName);

                if (!string.IsNullOrEmpty(depPackageId))
                {
                    dependencies.Add(_BuildPackageFromTargets(
                        depPackageId,
                        targetFramework,
                        projectAssets,
                        processedPackages,
                        false,
                        maxDepth,
                        currentDepth + 1));
                }
            }
        }

        // Remove from processed after building this branch to allow other branches to process it
        processedPackages.Remove(packageId);

        return new PackageReference
        {
            Name = packageName,
            Version = version,
            Type = "package",
            Dependencies = dependencies
        };
    }

    private string _GetTargetFrameworkKey(
        ProjectAssets projectAssets,
        string? targetFramework)
    {
        if (!string.IsNullOrEmpty(targetFramework))
        {
            var exactMatch = projectAssets.Targets.Keys
                .FirstOrDefault(key => key.Contains(targetFramework, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(exactMatch))
                return exactMatch;
        }

        // Return the first target framework key
        return projectAssets.Targets.Keys.FirstOrDefault() ?? string.Empty;
    }

    private TargetFramework? _GetTargetFramework(
        ProjectAssets projectAssets,
        string? targetFramework)
    {
        var key = _GetTargetFrameworkKey(projectAssets, targetFramework);
        if (string.IsNullOrEmpty(key))
            return null;

        return projectAssets.Targets.TryGetValue(key, out var target) ? target : null;
    }
}