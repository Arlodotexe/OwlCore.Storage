using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for <see cref="IModifiableFolder"/>.
/// </summary>
public static partial class ModifiableFolderExtensions
{
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
        using var destinationStream = await newFile.OpenStreamAsync(FileAccess.ReadWrite, cancellationToken: cancellationToken);

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
    /// Moves a storable item out of the source folder, and into the destination folder. Returns the new item that resides in this folder.
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