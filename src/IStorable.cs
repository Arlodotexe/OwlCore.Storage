namespace OwlCore.Storage;

/// <summary>
/// The absolute minimum requirements for all storable items.
/// </summary>
public interface IStorable
{
    /// <summary>
    /// A unique and consistent identifier for this file or folder. This dedicated resource identifier is used to identify the exact file you're pointing to.
    /// </summary>
    /// <remarks>
    /// Uri paths, especially those from cloud storage, can change regularly (e.g. when re-authenticating), and some files/folders aren't even addressable, meaning paths can't be used as a reliable content identifier.
    /// Also, custom and especially cloud file systems often use a predictable or custom ID as the primary-key in a flat or near-flat database table of files and folders. This also means that names aren't guaranteed to be unique within a folder.
    ///
    /// <para />
    /// Instead, since the underlying implementation knows the requirements, it can supply what data it has as long as it uniquely and consistently identifies the content.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// The name of the file or folder, with the extension (if any).
    /// </summary>
    string Name { get; } 
}