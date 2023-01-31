using System;
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
    /// <returns>An async enumerable which yields the files in the provided folder.</returns>
    public static IAsyncEnumerable<IAddressableFile> GetFilesAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.File, cancellationToken).Cast<IAddressableFile>();

    /// <summary>
    /// Retrieves all files from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="folder">The folder to get files from.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the folders in the provided folder.</returns>
    public static IAsyncEnumerable<IAddressableFolder> GetFoldersAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.Folder, cancellationToken).Cast<IAddressableFolder>();

    /// <summary>
    /// Retrieves the <see cref="IStorable"/> item which has the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="id">The <see cref="IStorable.Id"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the items in the provided folder.</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IAddressableStorable> GetItemAsync(this IFolder folder, string id, CancellationToken cancellationToken = default)
    {
        if (folder is IFolderCanFastGetItem fastPath)
            return await fastPath.GetItemAsync(id, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken).FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (targetItem is null)
            throw new FileNotFoundException($"No storage item with the ID \"{id}\" could be found.");

        return targetItem;
    }
    
    /// <summary>
    /// Retrieves the <see cref="IStorable"/> item which has the provided <paramref name="name"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="name">The <see cref="IStorable.Name"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the items in the provided folder.</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IAddressableStorable> GetItemByNameAsync(this IFolder folder, string name, CancellationToken cancellationToken = default)
    {
        if (folder is IFolderCanFastGetItemByName fastPath)
            return await fastPath.GetItemByNameAsync(name, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(x => name.Equals(x.Name, StringComparison.Ordinal), cancellationToken: cancellationToken);
        if (targetItem is null)
            throw new FileNotFoundException($"No storage item with the name \"{name}\" could be found.");

        return targetItem;
    }
}