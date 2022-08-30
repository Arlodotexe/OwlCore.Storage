using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Represents a folder that can be modified.
/// </summary>
public interface IModifiableFolder : IMutableFolder 
{
    /// <summary>
    /// Deletes the provided storable item from this folder.
    /// </summary>
    /// <param name="item">The item to be removed from this folder.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task DeleteAsync(IStorable item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a copy of the provided file within this folder.
    /// </summary>
    /// <param name="fileToCopy">The file to be copied into this folder.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task<IFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
    /// </summary>
    /// <param name="fileToMove">The file being moved into this folder.</param>
    /// <param name="source">The folder that <paramref name="fileToMove"/> is being moved from.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task<IFile> MoveFromAsync(IFile fileToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new folder with the desired name inside this folder.
    /// </summary>
    /// <param name="name">The name of the new folder.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task<IFolder> CreateFolderAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new file with the desired name inside this folder.
    /// </summary>
    /// <param name="name">The name of the new file.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task<IFile> CreateFileAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default);
}
