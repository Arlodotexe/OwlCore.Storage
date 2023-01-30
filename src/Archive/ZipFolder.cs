using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace OwlCore.Storage.Archive;

/// <summary>
/// A folder implementation wrapping a <see cref="ZipArchive"/>.
/// </summary>
public class ZipFolder : ReadOnlyZipFolder, IModifiableFolder, IFolderCanFastGetItem
{
    /// <summary>
    /// Creates a new instance of <see cref="ZipFolder"/>.
    /// </summary>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="sourceFile">The file that this archive originated from.</param>
    public ZipFolder(ZipArchive archive, IStorable sourceFile)
        : base(archive, sourceFile)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ZipFolder"/>.
    /// </summary>
    /// <remarks>
    /// This constructor is used internally for creating subfolders.
    /// </remarks>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="name">The name of this item</param>
    /// <param name="parent">The parent of this folder.</param>
    internal ZipFolder(ZipArchive archive, string name, ReadOnlyZipFolder parent)
        : base(archive, name, parent)
    {
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var srcStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (srcStream.CanSeek && srcStream.Position != 0)
            srcStream.Seek(0, SeekOrigin.Begin);
        else if (srcStream.Position != 0)
            throw new InvalidOperationException("The opened file stream is not at position 0 and cannot be seeked. Unable to copy.");

        var existingEntry = TryGetEntry($"{Path}{fileToCopy.Name}");
        if (!overwrite && existingEntry is not null)
            return new ZipEntryFile(existingEntry, this);

        var copy = await CreateFileAsync(fileToCopy.Name, overwrite, cancellationToken);
        using var dstStream = await copy.OpenStreamAsync(FileAccess.Write, cancellationToken);

        await srcStream.CopyToAsync(dstStream, 81920, cancellationToken);
        srcStream.Position = 0;

        return copy;
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var realSubPath = $"{Path}{name}";

        var entry = TryGetEntry(realSubPath);
        if (overwrite && entry is not null)
        {
            entry.Delete();
            entry = null;
        }

        entry ??= _archive.CreateEntry(realSubPath);

        return new ZipEntryFile(entry, this);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var subPath = $"{Path}{name}{ZIP_DIRECTORY_SEPARATOR}";
        GetVirtualFolders().TryGetValue(subPath, out var folder);

        if (overwrite && folder is not null)
        {
            await DeleteAsync(folder, cancellationToken);
            folder = null;
        }

        if (folder is null)
        {
            folder = new ZipFolder(_archive, name, this);
            GetVirtualFolders()[subPath] = folder;
        }

        return folder;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item is ZipFolder folder)
        {
            // Recursively remove any sub-entries
            // Pre-enumerate because the entries list will change in the loop
            var childEntries = _archive.Entries
                .Where(e => IsChild(e.FullName, folder.Path))
                .ToList();
            foreach (var entry in childEntries)
                entry.Delete();

            GetVirtualFolders().Remove(folder.Id);
        }
        else
        {
            var entry = TryGetEntry(item.Path);
            entry?.Delete();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolderWatcher>(new ZipFolderWatcher(this));
    }

    /// <inheritdoc/>
    public Task<IAddressableStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IAddressableStorable item;

        // Id can act as a Path (matches entry names) if we remove the prepended root folder Id.
        id = id.Replace(RootFolder.Id.TrimEnd(ZIP_DIRECTORY_SEPARATOR), string.Empty);

        var entry = TryGetEntry(id);
        if (entry is not null)
        {
            // Get file
            item = new ZipEntryFile(entry, this);
        }
        else
        {
            // Get folder
            item = GetVirtualFolders()[id];
        }

        return Task.FromResult(item);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> MoveFromAsync(IAddressableFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Zip archives can't move files around, so we have to take
        // the slower manual path every time.
        var file = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }
}
