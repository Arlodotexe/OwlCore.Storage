namespace OwlCore.Storage;

/// <summary>
/// The minimum representation of an item that can be stored or retrieved from storage.
/// </summary>
public interface IStorable
{
    /// <summary>
    /// A unique and consistent identifier for this file or folder. This dedicated resource identifier is used to identify the exact file or folder you're pointing to.
    /// </summary>
    /// <remarks>
    /// <para/> Custom and (especially cloud) file systems often use a flat or near-flat database and a predictable or custom ID as the primary-key, which can be used as an Id.
    /// <para/> Paths that are unique to the local file system can be used as an Id.
    /// <para/> Uri-based resource paths that change regularly (e.g. when re-authenticating) cannot be used as a reliable Id.
    /// <para/> Names aren't guaranteed to be non-empty or unique within a folder, and cannot be used as an Id.
    ///
    /// <para />
    /// The underlying implementation can use whatever string data it has to produce this ID, so long as it uniquely and consistently identifies this specific resource.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// The name of the file or folder, with the extension (if any).
    /// </summary>
    string Name { get; } 
}