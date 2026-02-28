using System;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item exposes a last accessed timestamp property with timezone offset information.
/// </summary>
/// <remarks>
/// Extends <see cref="ILastAccessedAt"/> to also provide <see cref="DateTimeOffset"/>-based access when the
/// underlying implementation can provide timezone offset information.
/// </remarks>
public interface ILastAccessedAtOffset : ILastAccessedAt
{
    /// <summary>
    /// Gets the last accessed timestamp property with timezone offset information.
    /// </summary>
    ILastAccessedAtOffsetProperty LastAccessedAtOffset { get; }
}
