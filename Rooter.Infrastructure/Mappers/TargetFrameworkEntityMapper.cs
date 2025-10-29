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

        // TargetFrameworkEntity is now a Dictionary<string, PackageReferenceEntity>
        // where the key is the package/version (e.g., "AutoMapper/12.0.0")
        foreach (var package in entity)
        {
            var packageRef = package.Value.ToDomain(package.Key);
            dependencies[package.Key] = packageRef;
        }

        return new TargetFramework
        {
            Dependencies = dependencies
        };
    }
}