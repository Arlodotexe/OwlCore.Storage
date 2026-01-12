using System;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item exposes a creation timestamp property with timezone offset information.
/// </summary>
/// <remarks>
/// Extends <see cref="ICreatedAt"/> to also provide <see cref="DateTimeOffset"/>-based access when the
/// underlying implementation can provide timezone offset information.
/// </remarks>
public interface ICreatedAtOffset : ICreatedAt
{
    /// <summary>
    /// Gets the creation timestamp property with timezone offset information.
    /// </summary>
    ICreatedAtOffsetProperty CreatedAtOffset { get; }
}
