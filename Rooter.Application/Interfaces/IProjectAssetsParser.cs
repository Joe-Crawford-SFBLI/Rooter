using Rooter.Domain.Entities;

namespace Rooter.Application.Interfaces;

/// <summary>
/// Interface for parsing project.assets.json files.
/// This interface is defined in the Application layer to establish the contract
/// with the Infrastructure layer while keeping implementation details abstracted.
/// </summary>
public interface IProjectAssetsParser
{
    /// <summary>
    /// Parses a project.assets.json file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the project.assets.json file</param>
    /// <returns>The parsed project assets data, or null if parsing fails</returns>
    Task<ProjectAssets?> ParseAsync(string filePath);

    /// <summary>
    /// Parses project.assets.json content from a JSON string.
    /// </summary>
    /// <param name="jsonContent">The JSON content of the project.assets.json file</param>
    /// <returns>The parsed project assets data, or null if parsing fails</returns>
    Task<ProjectAssets?> ParseFromJsonAsync(string jsonContent);

    /// <summary>
    /// Extracts package references from parsed project assets.
    /// </summary>
    /// <param name="projectAssets">The parsed project assets</param>
    /// <param name="targetFramework">Optional target framework to filter by</param>
    /// <returns>A list of package references</returns>
    List<PackageReference> ExtractPackages(
        ProjectAssets projectAssets,
        string? targetFramework = null);
}