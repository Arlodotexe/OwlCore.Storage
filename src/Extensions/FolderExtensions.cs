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
    public static IAsyncEnumerable<IChildFile> GetFilesAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.File, cancellationToken).Cast<IChildFile>();

    /// <summary>
    /// Retrieves all files from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="folder">The folder to get files from.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the folders in the provided folder.</returns>
    public static IAsyncEnumerable<IChildFolder> GetFoldersAsync(this IFolder folder, CancellationToken cancellationToken = default) => folder.GetItemsAsync(StorableType.Folder, cancellationToken).Cast<IChildFolder>();

    /// <summary>
    /// Retrieves the <see cref="IStorable"/> item which has the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="id">The <see cref="IStorable.Id"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the items in the provided folder.</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IStorableChild> GetItemAsync(this IFolder folder, string id, CancellationToken cancellationToken = default)
    {
        if (folder is IFastGetItem fastPath)
            return await fastPath.GetItemAsync(id, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken).FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (targetItem is null)
            throw new FileNotFoundException($"No storage item with the ID \"{id}\" could be found.");

        return targetItem;
    }

    /// <summary>
    /// Retrieves the first <see cref="IStorable"/> item which has the provided <paramref name="name"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="name">The <see cref="IStorable.Name"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the items in the provided folder.</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IStorableChild> GetFirstByNameAsync(this IFolder folder, string name, CancellationToken cancellationToken = default)
    {
        if (folder is IFastGetFirstByName fastPath)
            return await fastPath.GetFirstByNameAsync(name, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(x => name.Equals(x.Name, StringComparison.Ordinal), cancellationToken: cancellationToken);

        if (targetItem is null)
            throw new FileNotFoundException($"No storage item with the name \"{name}\" could be found.");

        return targetItem;
    }

    /// <summary>
    /// Creates a copy of the provided file within this folder.
    /// </summary>
    /// <param name="destinationFolder">The folder where the copy is created.</param>
    /// <param name="fileToCopy">The file to be copied into this folder.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public static async Task<IChildFile> CreateCopyOfAsync<T>(this IModifiableFolder destinationFolder, T fileToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
            where T : IFile
    {
        // If the destination folder can copy this file faster than us, use that.
        if (destinationFolder is IFastFileCopy<T> fastPath)
            return await fastPath.CreateCopyOfAsync(fileToCopy, overwrite, cancellationToken);

        // Open the source file
        using var sourceStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);

        // Create the destination file
        var newFile = await destinationFolder.CreateFileAsync(fileToCopy.Name, overwrite, cancellationToken);
        using var destinationStream = await newFile.OpenStreamAsync(cancellationToken: cancellationToken);

        // Align stream positions (if possible)
        if (destinationStream.CanSeek && destinationStream.Position != 0)
            destinationStream.Seek(0, SeekOrigin.Begin);

        if (sourceStream.CanSeek && sourceStream.Position != 0)
            sourceStream.Seek(0, SeekOrigin.Begin);

        // Copy the src into the dest file
        await sourceStream.CopyToAsync(destinationStream, bufferSize: 81920, cancellationToken);

        return newFile;
    }

    /// <summary>
    /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
    /// </summary>
    /// <param name="destinationFolder">The folder where the file is moved to.</param>
    /// <param name="fileToMove">The file being moved into this folder.</param>
    /// <param name="source">The folder that <paramref name="fileToMove"/> is being moved from.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public static async Task<IChildFile> MoveFromAsync<T>(this IModifiableFolder destinationFolder, T fileToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
        where T : IFile, IStorableChild
    {
        // If the destination folder can move this file faster than us, use that.
        if (destinationFolder is IFastFileMove<T> fastPath)
            return await fastPath.MoveFromAsync(fileToMove, source, overwrite, cancellationToken);

        // Manual move. Slower, but covers all scenarios.
        var file = await destinationFolder.CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }
}