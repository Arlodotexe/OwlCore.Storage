using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for <see cref="IFolder"/>.
/// </summary>
public static class FolderExtensions
{
    /// <summary>
    /// Retrieves all files from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="folder">The folder to get files from.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns></returns>
    public static IAsyncEnumerable<IFile> GetFilesAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.File, cancellationToken).Cast<IFile>();

    /// <summary>
    /// Retrieves all files from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="folder">The folder to get files from.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns></returns>
    public static IAsyncEnumerable<IFolder> GetFoldersAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.Folder, cancellationToken).Cast<IFolder>();
}

