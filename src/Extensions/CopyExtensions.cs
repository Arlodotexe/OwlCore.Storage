using System;
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
        cancellationToken.ThrowIfCancellationRequested();

        // If the destination folder declares a non-fallback copy path, try that.
        // Provide fallback in case this file is not a handled type.
        if (destinationFolder is ICreateCopyOf fastPath)
            return await fastPath.CreateCopyOfAsync(fileToCopy, overwrite, cancellationToken, fallback: CreateCopyOfFallbackAsync);

        // Manual copy. Slower, but covers all scenarios.
        return await CreateCopyOfFallbackAsync(destinationFolder, fileToCopy, overwrite, cancellationToken);
    }

    /// <summary>
    /// Creates a copy of the provided file within this folder.
    /// </summary>
    /// <param name="destinationFolder">The folder where the copy is created.</param>
    /// <param name="fileToCopy">The file to be copied into this folder.</param>
    /// <param name="newName">The name to use for the created file.</param>
    /// <param name="overwrite"><code>true</code> if any existing destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="FileAlreadyExistsException">Thrown when <paramref name="overwrite"/> is false and the resource being created already exists.</exception>
    public static async Task<IChildFile> CreateCopyOfAsync(this IModifiableFolder destinationFolder, IFile fileToCopy, bool overwrite, string newName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If the destination folder declares a non-fallback copy path, try that.
        // Provide fallback in case this file is not a handled type.
        if (destinationFolder is ICreateRenamedCopyOf fastPath)
            return await fastPath.CreateCopyOfAsync(fileToCopy, overwrite, newName, cancellationToken, fallback: CreateCopyOfFallbackAsync);

        // Manual copy. Slower, but covers all scenarios.
        return await CreateCopyOfFallbackAsync(destinationFolder, fileToCopy, overwrite, newName, cancellationToken);
    }

    internal static Task<IChildFile> CreateCopyOfFallbackAsync(IModifiableFolder destinationFolder, IFile fileToCopy, bool overwrite, CancellationToken cancellationToken = default)
        => CreateCopyOfFallbackAsync(destinationFolder, fileToCopy, overwrite, fileToCopy.Name, cancellationToken);

    internal static async Task<IChildFile> CreateCopyOfFallbackAsync(IModifiableFolder destinationFolder, IFile fileToCopy, bool overwrite, string newName, CancellationToken cancellationToken = default)
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

        // Capture LastModifiedAt BEFORE opening streams
        // Copy semantics: only LastModifiedAt is preserved (CreatedAt and LastAccessedAt get current time)
        DateTime? sourceLastModifiedAt = null;
        DateTimeOffset? sourceLastModifiedAtOffset = null;

        if (fileToCopy is ILastModifiedAt srcLastModifiedAt)
        {
            try { sourceLastModifiedAt = await srcLastModifiedAt.LastModifiedAt.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        if (fileToCopy is ILastModifiedAtOffset srcLastModifiedAtOffset)
        {
            try { sourceLastModifiedAtOffset = await srcLastModifiedAtOffset.LastModifiedAtOffset.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        // Create the destination file.
        // 'overwrite: false' would have thrown above if the file exists, so either overwrite is already true or the file doesn't exist yet.
        // Always overwrite here so the file is empty.
        var newFile = await destinationFolder.CreateFileAsync(newName, overwrite: true, cancellationToken);

        // Copy file content
        using (var destinationStream = await newFile.OpenStreamAsync(FileAccess.Write, cancellationToken: cancellationToken))
        using (var sourceStream = await fileToCopy.OpenStreamAsync(FileAccess.Read, cancellationToken: cancellationToken))
        {
            await sourceStream.CopyToAsync(destinationStream, bufferSize: 81920, cancellationToken);
        }

        // Apply only LastModifiedAt to destination (matches native copy behavior)
        if (sourceLastModifiedAt.HasValue && newFile is ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> destLastModifiedAt })
        {
            try { await destLastModifiedAt.UpdateValueAsync(sourceLastModifiedAt.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        if (sourceLastModifiedAtOffset.HasValue && newFile is ILastModifiedAtOffset { LastModifiedAtOffset: IModifiableStorageProperty<DateTimeOffset?> destLastModifiedAtOffset })
        {
            try { await destLastModifiedAtOffset.UpdateValueAsync(sourceLastModifiedAtOffset.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        return newFile;
    }

    /// <summary>
    /// Copies the contents of the source file to the destination file.
    /// </summary>
    /// <param name="sourceFile">The source file to copy from.</param>
    /// <param name="destinationFile">The destination file to copy to.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    public static async Task CopyToAsync(this IFile sourceFile, IFile destinationFile, CancellationToken cancellationToken = default)
    {
        using var sourceStream = await sourceFile.OpenStreamAsync(FileAccess.Read, cancellationToken);
        using var destinationStream = await destinationFile.OpenStreamAsync(FileAccess.Write, cancellationToken);

#if NETSTANDARD
        await sourceStream.CopyToAsync(destinationStream);
#elif NET5_OR_GREATER
        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
#endif
    }
}
