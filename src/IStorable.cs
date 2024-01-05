namespace OwlCore.Storage;

/// <summary>
/// Represents an item that can be stored or retrieved from a storage source.
/// </summary>
public interface IStorable
{
    /// <summary>
    /// Gets a unique identifier for this item that is consistent across reruns.
    /// </summary>
    /// <remarks>
    /// <para/> Custom and (especially cloud) file systems often use a flat or near-flat database and a predictable or custom ID as the primary-key, which can be used as an Id.
    /// <para/> Paths that are unique to the local file system can be used as an Id.
    /// <para/> Uri-based resource paths that change (e.g. when re-authenticating) should not be used as an Id.
    /// <para/> Names aren't guaranteed to be non-empty or unique within or across folders, and should not be used as an Id.
    ///
    /// <para />
    /// The implementation can use any string data available to produce this ID, so long as it identifies this specific resource across runs.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the name of the item, with the extension (if any).
    /// </summary>
    string Name { get; } 
}