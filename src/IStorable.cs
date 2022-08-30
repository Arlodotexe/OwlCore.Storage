namespace OwlCore.Storage;

public interface IStorable
{
    /// <summary>
    /// A unique and consistent identifier for this file or folder.
    /// </summary>
    /// <remarks>
    /// Not all files are addressable by path.
    /// Uri paths (like OneDrive) can change entirely when re-authenticating.
    /// Therefore, we need a dedicated resource identifier.
    /// </remarks>
    public string Id { get; }

    /// <summary>
    /// The name of the file or folder, with the extension (if any).
    /// </summary>
    public string Name { get; } 
}