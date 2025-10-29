using Rooter.Domain.Entities;
using Rooter.Infrastructure.Entities;

namespace Rooter.Infrastructure.Mappers;

/// <summary>
/// Maps infrastructure entities to domain entities for libraries.
/// </summary>
public static class LibraryEntityMapper
{
    /// <summary>
    /// Maps a LibraryEntity to a Library domain entity.
    /// </summary>
    /// <param name="entity">The infrastructure entity to map</param>
    /// <returns>The mapped domain entity</returns>
    public static Library ToDomain(
        this LibraryEntity entity)
    {
        return new Library
        {
            Type = entity.Type,
            Serviceable = entity.Serviceable,
            Sha512 = entity.Sha512,
            Path = entity.Path,
            Files = entity.Files ?? []
        };
    }
}
