using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

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
    /// <param name="stream">The stream containing the ZIP archive.</param>
    /// <param name="storable">A storable containing the ID and name to use for this folder.</param>
    /// <param name="path">The relative path inside the ZIP archive. Leave empty for the root folder.</param>
    public ZipFolder(Stream stream, IStorable storable, string path = "")
        : base(stream, storable, path)
    {
    }
    
    protected ZipFolder(Stream zipStream, string rootId, string path, ZipArchiveMode mode = ZipArchiveMode.Read)
        : base(zipStream, rootId, path, mode)
    {
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var srcStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (srcStream.CanSeek)
            srcStream.Seek(0, SeekOrigin.Begin);
        else if (srcStream.Position != 0)
            throw new InvalidOperationException("The opened file stream is not at position 0 and cannot be seeked. Unable to copy.");

        var existingEntry = TryGetEntry(fileToCopy.Id);
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

        string realSubPath = Path + name;

        ZipArchiveEntry? entry = TryGetEntry(realSubPath);

        if (overwrite && entry is not null)
        {
            entry.Delete();
            entry = null;
        }

        entry ??= GetArchive().CreateEntry(realSubPath);

        return new ZipEntryFile(entry, this);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string subPath = NormalizeEnding(Path + name);
        bool exists = _virtualFolders.TryGetValue(subPath, out ZipFolder? folder);

        if (overwrite && exists)
        {
            await DeleteAsync(folder, cancellationToken);
            folder = null;
        }

        if (folder is null)
        {
            SimpleStorableItem storable = new(Id + name + ZIP_DIRECTORY_SEPARATOR, name);
            folder = new ZipFolder(_zipStream, storable, subPath);
            _virtualFolders[folder.Path] = folder;
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
            foreach (var entry in GetArchive().Entries.Where(e => e.FullName.StartsWith(folder.Path)))
                entry.Delete();

            _virtualFolders.Remove(folder.Id);
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

        string itemPath = Path + id;

        var entry = TryGetEntry(itemPath);
        if (entry is not null)
        {
            item = new ZipEntryFile(entry, this);
        }
        else
        {
            itemPath = NormalizeEnding(itemPath);
            if (_virtualFolders.TryGetValue(itemPath, out var existingFolder))
            {
                item = existingFolder;
            }
            else
            {
                SimpleStorableItem itemStorable = new(id, IOPath.GetFileNameWithoutExtension(itemPath));
                item = new ZipFolder(_stream, itemStorable, itemPath);
            }
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
