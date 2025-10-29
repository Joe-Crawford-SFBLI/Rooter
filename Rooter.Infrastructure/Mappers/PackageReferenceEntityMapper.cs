using Rooter.Domain.Entities;
using Rooter.Infrastructure.Entities;

namespace Rooter.Infrastructure.Mappers;

/// <summary>
/// Maps infrastructure entities to domain entities for package references.
/// </summary>
public static class PackageReferenceEntityMapper
{
    /// <summary>
    /// Maps a PackageReferenceEntity to a PackageReference domain entity.
    /// </summary>
    /// <param name="entity">The infrastructure entity to map</param>
    /// <param name="packageName">The name of the package</param>
    /// <returns>The mapped domain entity</returns>
    public static PackageReference ToDomain(
        this PackageReferenceEntity entity,
        string packageName)
    {
        var name = _ExtractPackageName(packageName);
        var version = _ExtractPackageVersion(packageName);

        var dependencies = new List<PackageReference>();
        if (entity.Dependencies != null)
        {
            foreach (var dep in entity.Dependencies)
            {
                var depName = _ExtractPackageName(dep.Key);
                var depVersion = dep.Value;

                dependencies.Add(new PackageReference
                {
                    Name = depName,
                    Version = depVersion,
                    Type = "package"
                });
            }
        }

        return new PackageReference
        {
            Name = name,
            Version = version,
            Type = entity.Type ?? "package",
            Dependencies = dependencies
        };
    }

    private static string _ExtractPackageName(
        string packageId)
    {
        var parts = packageId.Split('/');
        return parts.Length > 0 ? parts[0] : packageId;
    }

    private static string _ExtractPackageVersion(
        string packageId)
    {
        var parts = packageId.Split('/');
        return parts.Length > 1 ? parts[1] : "1.0.0";
    }
}