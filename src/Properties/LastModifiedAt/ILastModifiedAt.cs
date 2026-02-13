using System;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item exposes a last modified timestamp property.
/// </summary>
public interface ILastModifiedAt
{
    /// <summary>
    /// Gets the last modified timestamp property for this storage item.
    /// </summary>
    ILastModifiedAtProperty LastModifiedAt { get; }
}
