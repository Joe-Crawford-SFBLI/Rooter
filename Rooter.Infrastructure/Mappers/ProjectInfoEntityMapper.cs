using Rooter.Domain.Entities;
using Rooter.Infrastructure.Entities;

namespace Rooter.Infrastructure.Mappers;

/// <summary>
/// Maps infrastructure entities to domain entities for project information.
/// </summary>
public static class ProjectInfoEntityMapper
{
    /// <summary>
    /// Maps a ProjectInfoEntity to a ProjectInfo domain entity.
    /// </summary>
    /// <param name="entity">The infrastructure entity to map</param>
    /// <returns>The mapped domain entity</returns>
    public static ProjectInfo ToDomain(
        this ProjectInfoEntity entity)
    {
        var properties = new Dictionary<string, object>();

        if (entity.Restore != null)
        {
            if (!string.IsNullOrEmpty(entity.Restore.ProjectName))
                properties["ProjectName"] = entity.Restore.ProjectName;
            if (!string.IsNullOrEmpty(entity.Restore.ProjectPath))
                properties["ProjectPath"] = entity.Restore.ProjectPath;
        }

        return new ProjectInfo
        {
            Version = entity.Version,
            Restore = entity.Restore != null,
            Properties = properties
        };
    }
}