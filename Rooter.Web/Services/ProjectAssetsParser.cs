using System.Text.Json;
using Rooter.Web.Models;

namespace Rooter.Web.Services;

public interface IProjectAssetsParser
{
    Task<ProjectAssets?> ParseAsync(string filePath);
    Task<ProjectAssets?> ParseFromJsonAsync(string jsonContent);
    List<PackageReference> ExtractPackages(ProjectAssets projectAssets, string? targetFramework = null);
}

public class ProjectAssetsParser : IProjectAssetsParser
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ProjectAssetsParser()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ProjectAssets?> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var jsonContent = await File.ReadAllTextAsync(filePath);
        return await ParseFromJsonAsync(jsonContent);
    }

    public Task<ProjectAssets?> ParseFromJsonAsync(string jsonContent)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            var projectAssets = new ProjectAssets
            {
                Version = root.GetProperty("version").GetInt32()
            };

            // Parse targets
            if (root.TryGetProperty("targets", out var targetsElement))
            {
                foreach (var targetProperty in targetsElement.EnumerateObject())
                {
                    var targetFramework = targetProperty.Name;
                    var packages = new Dictionary<string, PackageTarget>();

                    foreach (var packageProperty in targetProperty.Value.EnumerateObject())
                    {
                        var packageTarget = ParsePackageTarget(packageProperty.Value);
                        packages[packageProperty.Name] = packageTarget;
                    }

                    projectAssets.Targets[targetFramework] = packages;
                }
            }

            // Parse libraries
            if (root.TryGetProperty("libraries", out var librariesElement))
            {
                foreach (var libraryProperty in librariesElement.EnumerateObject())
                {
                    var library = ParseLibrary(libraryProperty.Value);
                    projectAssets.Libraries[libraryProperty.Name] = library;
                }
            }

            return Task.FromResult<ProjectAssets?>(projectAssets);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse project.assets.json: {ex.Message}", ex);
        }
    }

    public List<PackageReference> ExtractPackages(ProjectAssets projectAssets, string? targetFramework = null)
    {
        var packages = new List<PackageReference>();

        // Use the first target framework if none specified
        var target = targetFramework != null && projectAssets.Targets.ContainsKey(targetFramework)
            ? projectAssets.Targets[targetFramework]
            : projectAssets.Targets.Values.FirstOrDefault();

        if (target == null)
            return packages;

        foreach (var packageKvp in target)
        {
            var packageId = packageKvp.Key;
            var packageTarget = packageKvp.Value;

            // Skip project references
            if (packageTarget.Type == "project")
                continue;

            var parts = packageId.Split('/');
            if (parts.Length != 2)
                continue;

            var packageName = parts[0];
            var version = parts[1];

            var dependencies = packageTarget.Dependencies
                .Select(dep => new PackageReference
                {
                    Name = dep.Key,
                    Version = dep.Value,
                    Type = "package"
                })
                .ToList();

            var package = new PackageReference
            {
                Name = packageName,
                Version = version,
                Type = packageTarget.Type,
                Dependencies = dependencies
            };

            packages.Add(package);
        }

        return packages;
    }

    private PackageTarget ParsePackageTarget(JsonElement element)
    {
        var target = new PackageTarget();

        if (element.TryGetProperty("type", out var typeElement))
            target.Type = typeElement.GetString() ?? "";

        if (element.TryGetProperty("dependencies", out var dependenciesElement))
        {
            foreach (var depProperty in dependenciesElement.EnumerateObject())
            {
                target.Dependencies[depProperty.Name] = depProperty.Value.GetString() ?? "";
            }
        }

        if (element.TryGetProperty("compile", out var compileElement))
        {
            foreach (var compileProperty in compileElement.EnumerateObject())
            {
                target.Compile[compileProperty.Name] = compileProperty.Value;
            }
        }

        if (element.TryGetProperty("runtime", out var runtimeElement))
        {
            foreach (var runtimeProperty in runtimeElement.EnumerateObject())
            {
                target.Runtime[runtimeProperty.Name] = runtimeProperty.Value;
            }
        }

        if (element.TryGetProperty("frameworkReferences", out var frameworkRefsElement))
        {
            target.FrameworkReferences = frameworkRefsElement.EnumerateArray()
                .Select(x => x.GetString() ?? "")
                .ToList();
        }

        return target;
    }

    private Library ParseLibrary(JsonElement element)
    {
        var library = new Library();

        if (element.TryGetProperty("sha512", out var sha512Element))
            library.Sha512 = sha512Element.GetString() ?? "";

        if (element.TryGetProperty("type", out var typeElement))
            library.Type = typeElement.GetString() ?? "";

        if (element.TryGetProperty("path", out var pathElement))
            library.Path = pathElement.GetString() ?? "";

        if (element.TryGetProperty("files", out var filesElement))
        {
            foreach (var fileProperty in filesElement.EnumerateArray())
            {
                var fileName = fileProperty.GetString() ?? "";
                if (!string.IsNullOrEmpty(fileName))
                    library.Files[fileName] = fileName;
            }
        }

        return library;
    }
}