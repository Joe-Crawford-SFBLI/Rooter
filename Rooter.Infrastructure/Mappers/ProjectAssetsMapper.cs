using Rooter.Domain.Entities;
using Rooter.Infrastructure.Entities;

namespace Rooter.Infrastructure.Mappers;

/// <summary>
/// Maps infrastructure entities to domain entities for project assets.
/// </summary>
public static class ProjectAssetsMapper
{
    /// <summary>
    /// Maps a ProjectAssetsEntity to a ProjectAssets domain entity.
    /// </summary>
    /// <param name="entity">The infrastructure entity to map</param>
    /// <returns>The mapped domain entity</returns>
    public static ProjectAssets ToDomain(
        this ProjectAssetsEntity entity)
    {
        var targets = entity.Targets
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDomain());

        var libraries = entity.Libraries
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDomain());

        var projectFileDependencyGroups = entity.ProjectFileDependencyGroups
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());

        return new ProjectAssets
        {
            Version = entity.Version,
            Targets = targets,
            Libraries = libraries,
            Project = entity.Project?.ToDomain(),
            ProjectFileDependencyGroups = projectFileDependencyGroups
        };
    }
}