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
    /// Moves a storable item out of the source folder, and into the destination folder. Returns the new item that resides in this folder.
    /// </summary>
    /// <param name="destinationFolder">The folder where the file is moved to.</param>
    /// <param name="fileToMove">The file being moved into this folder.</param>
    /// <param name="source">The folder that <paramref name="fileToMove"/> is being moved from.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="FileAlreadyExistsException">Thrown when <paramref name="overwrite"/> is false and the resource being created already exists.</exception>
    public static async Task<IChildFile> MoveFromAsync(this IModifiableFolder destinationFolder, IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If the destination file exists and overwrite is false, it shouldn't be overwritten or returned as-is. Throw an exception instead.
        if (!overwrite)
        {
            try
            {
                var existing = await destinationFolder.GetFirstByNameAsync(fileToMove.Name, cancellationToken);
                if (existing is not null)
                    throw new FileAlreadyExistsException(fileToMove.Name);
            }
            catch (FileNotFoundException) { }
        }

        // If the destination folder declares a non-fallback move path, try that.
        // Provide fallback in case this file is not a handled type.
        if (destinationFolder is IMoveFrom fastPath)
            return await fastPath.MoveFromAsync(fileToMove, source, overwrite, cancellationToken, fallback: MoveFromFallbackAsync);

        // Manual move. Slower, but covers all scenarios.
        return await MoveFromFallbackAsync(destinationFolder, fileToMove, source, overwrite, cancellationToken);
    }

    /// <summary>
    /// Moves a storable item out of the source folder, and into the destination folder. Returns the new item that resides in this folder.
    /// </summary>
    /// <param name="destinationFolder">The folder where the file is moved to.</param>
    /// <param name="fileToMove">The file being moved into this folder.</param>
    /// <param name="source">The folder that <paramref name="fileToMove"/> is being moved from.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="newName">The name to use for the created file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="FileAlreadyExistsException">Thrown when <paramref name="overwrite"/> is false and the resource being created already exists.</exception>
    public static async Task<IChildFile> MoveFromAsync(this IModifiableFolder destinationFolder, IChildFile fileToMove, IModifiableFolder source, bool overwrite, string newName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If the destination file exists and overwrite is false, it shouldn't be overwritten or returned as-is. Throw an exception instead.
        if (!overwrite)
        {
            try
            {
                var existing = await destinationFolder.GetFirstByNameAsync(newName, cancellationToken);
                if (existing is not null)
                    throw new FileAlreadyExistsException(newName);
            }
            catch (FileNotFoundException) { }
        }

        // If the destination folder declares a non-fallback move path, try that.
        // Provide fallback in case this file is not a handled type.
        if (destinationFolder is IMoveRenamedFrom fastPath)
            return await fastPath.MoveFromAsync(fileToMove, source, overwrite, newName, cancellationToken, fallback: MoveFromFallbackAsync);

        // Manual move. Slower, but covers all scenarios.
        return await MoveFromFallbackAsync(destinationFolder, fileToMove, source, overwrite, newName, cancellationToken);
    }

    private static async Task<IChildFile> MoveFromFallbackAsync(IModifiableFolder destinationFolder, IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken = default)
    {
        var file = await destinationFolder.CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }

    private static async Task<IChildFile> MoveFromFallbackAsync(IModifiableFolder destinationFolder, IChildFile fileToMove, IModifiableFolder source, bool overwrite, string newName, CancellationToken cancellationToken = default)
    {
        var file = await destinationFolder.CreateCopyOfAsync(fileToMove, overwrite, newName, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }
}