namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item exposes a creation timestamp property.
/// </summary>
public interface ICreatedAt
{
    /// <summary>
    /// Gets the creation timestamp property for this storage item.
    /// </summary>
    ICreatedAtProperty CreatedAt { get; }
}
