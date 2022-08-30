using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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


    /// <summary>
    /// Retrieves the <see cref="IStorable"/> item which has the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="id">The <see cref="IStorable.Id"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The relevant</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IStorable> GetItemAsync(this IFolder folder, string id, CancellationToken cancellationToken = default)
    {
        if (folder is IFolderCanFastGetItem fastPath)
            return await fastPath.GetItemAsync(id, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken).FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (targetItem is null)
            throw new FileNotFoundException($"No storage item with the ID \"{id}\" could be found.");

        return targetItem;
    }
}