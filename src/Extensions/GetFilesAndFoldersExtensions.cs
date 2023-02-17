using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for <see cref="IFolder"/>.
/// </summary>
public static partial class FolderExtensions
{
    /// <summary>
    /// Retrieves all files from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="folder">The folder to get files from.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the files in the provided folder.</returns>
    public static IAsyncEnumerable<IChildFile> GetFilesAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.File, cancellationToken).Cast<IChildFile>();

    /// <summary>
    /// Retrieves all files from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="folder">The folder to get files from.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the folders in the provided folder.</returns>
    public static IAsyncEnumerable<IChildFolder> GetFoldersAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.Folder, cancellationToken).Cast<IChildFolder>();
}