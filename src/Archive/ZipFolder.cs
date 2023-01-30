using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    /// <param name="archive">A ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="storable">A storable containing the ID, name, and path of this folder.</param>
    /// <param name="parent">The parent of this folder, if one exists.</param>
    public ZipFolder(ZipArchive archive, SimpleZipStorableItem storable, IFolder? parent = null)
        : base(archive, storable, parent)
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

        entry ??= _archive.CreateEntry(realSubPath);

        return new ZipEntryFile(entry, this);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string subPath = Path + name + ZIP_DIRECTORY_SEPARATOR;
        GetVirtualFolders().TryGetValue(subPath, out var folder);

        if (overwrite && folder is not null)
        {
            await DeleteAsync(folder, cancellationToken);
            folder = null;
        }

        if (folder is null)
        {
            var storable = SimpleZipStorableItem.CreateFromParentId(Id, name, true);
            folder = CreateSubfolderItem(_archive, storable);
            GetVirtualFolders()[folder.Path] = folder;
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
            foreach (var entry in _archive.Entries.Where(e => IsChild(e.FullName, folder.Path)))
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
        SimpleZipStorableItem storable = new(id);

        var entry = TryGetEntry(storable.Path);
        if (entry is not null)
        {
            item = new ZipEntryFile(entry, this);
        }
        else
        {
            storable.ChangeStorableType(true);
            item = GetVirtualFolders()[storable.Path];
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

    /// <inheritdoc/>
    protected override IAddressableFolder CreateSubfolderItem(ZipArchive archive, SimpleZipStorableItem storable, IFolder? parent = null)
    {
        return new ZipFolder(archive, storable, parent);
    }
}
