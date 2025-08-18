using System.IO;
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
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new folder with the desired name inside this folder.
    /// </summary>
    /// <param name="name">The name of the new folder.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The newly created (or opened if existing) folder.</returns>
    Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new file with the desired name inside this folder.
    /// </summary>
    /// <param name="name">The name of the new file.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The newly created (or opened if existing) file.</returns>
    Task<IChildFile> CreateFileAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default);
}
