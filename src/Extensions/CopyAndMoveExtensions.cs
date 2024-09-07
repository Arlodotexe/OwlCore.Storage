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
    /// <param name="overwrite"><code>true</code> if any existing destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="FileAlreadyExistsException">Thrown when <paramref name="overwrite"/> is false and the resource being created already exists.</exception>
    public static async Task<IChildFile> CreateCopyOfAsync(this IModifiableFolder destinationFolder, IFile fileToCopy, bool overwrite, CancellationToken cancellationToken = default)
    {
        static async Task<IChildFile> CreateCopyOfFallbackAsync(IModifiableFolder destinationFolder, IFile fileToCopy, bool overwrite, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the destination file exists and overwrite is false, it shouldn't be overwritten or returned as-is. Throw an exception instead.
            if (!overwrite)
            {
                try
                {
                    var existing = await destinationFolder.GetFirstByNameAsync(fileToCopy.Name, cancellationToken);
                    if (existing is not null)
                        throw new FileAlreadyExistsException(fileToCopy.Name);
                }
                catch (FileNotFoundException) { }
            }

            // Create the destination file.
            // 'overwrite: false' would have thrown above if the file exists, so either overwrite is already true or the file doesn't exist yet.
            // Always overwrite here so the file is empty. 
            var newFile = await destinationFolder.CreateFileAsync(fileToCopy.Name, overwrite: true, cancellationToken);
            using var destinationStream = await newFile.OpenStreamAsync(FileAccess.Write, cancellationToken: cancellationToken);

            // Open the source file
            using var sourceStream = await fileToCopy.OpenStreamAsync(FileAccess.Read, cancellationToken: cancellationToken);

            // Copy the src into the dest file
            await sourceStream.CopyToAsync(destinationStream, bufferSize: 81920, cancellationToken);

            return newFile;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // If the destination folder declares a non-fallback copy path, try that.
        // Provide fallback in case this file is not a handled type.
        if (destinationFolder is ICreateCopyOf fastPath)
            return await fastPath.CreateCopyOfAsync(fileToCopy, overwrite, cancellationToken, fallback: CreateCopyOfFallbackAsync);

        // Manual copy. Slower, but covers all scenarios.
        return await CreateCopyOfFallbackAsync(destinationFolder, fileToCopy, overwrite, cancellationToken);

    }

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
        static async Task<IChildFile> MoveFromFallbackAsync(IModifiableFolder destinationFolder, IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken = default)
        {
            var file = await destinationFolder.CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
            await source.DeleteAsync(fileToMove, cancellationToken);

            return file;
        }

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
}