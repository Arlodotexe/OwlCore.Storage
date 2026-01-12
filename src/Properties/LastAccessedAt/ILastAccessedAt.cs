namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item exposes a last accessed timestamp property.
/// </summary>
public interface ILastAccessedAt
{
    /// <summary>
    /// Gets the last accessed timestamp property for this storage item.
    /// </summary>
    ILastAccessedAtProperty LastAccessedAt { get; }
}
