using Rooter.Domain.Entities;
using Rooter.Infrastructure.Entities;

namespace Rooter.Infrastructure.Mappers;

/// <summary>
/// Maps infrastructure entities to domain entities for target frameworks.
/// </summary>
public static class TargetFrameworkEntityMapper
{
    /// <summary>
    /// Maps a TargetFrameworkEntity to a TargetFramework domain entity.
    /// </summary>
    /// <param name="entity">The infrastructure entity to map</param>
    /// <returns>The mapped domain entity</returns>
    public static TargetFramework ToDomain(
        this TargetFrameworkEntity entity)
    {
        var dependencies = new Dictionary<string, PackageReference>();

        if (entity.Dependencies != null)
        {
            foreach (var dep in entity.Dependencies)
            {
                var packageRef = dep.Value.ToDomain(dep.Key);
                dependencies[dep.Key] = packageRef;
            }
        }

        return new TargetFramework
        {
            Dependencies = dependencies
        };
    }
}