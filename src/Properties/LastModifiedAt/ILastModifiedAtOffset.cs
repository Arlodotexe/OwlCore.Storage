using System;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item exposes a last modified timestamp property with timezone offset information.
/// </summary>
/// <remarks>
/// Extends <see cref="ILastModifiedAt"/> to also provide <see cref="DateTimeOffset"/>-based access when the
/// underlying implementation can provide timezone offset information.
/// </remarks>
public interface ILastModifiedAtOffset : ILastModifiedAt
{
    /// <summary>
    /// Gets the last modified timestamp property with timezone offset information.
    /// </summary>
    ILastModifiedAtOffsetProperty LastModifiedAtOffset { get; }
}
