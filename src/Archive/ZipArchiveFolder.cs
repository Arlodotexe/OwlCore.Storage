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
public class ZipArchiveFolder : ReadOnlyZipArchiveFolder, IModifiableFolder, IFolderCanFastGetItem
{
    /// <summary>
    /// Creates a new instance of <see cref="ZipArchiveFolder"/>.
    /// </summary>
    /// <param name="sourceFile">The Id and Name of the "source file" that created this item.</param>
    public ZipArchiveFolder(IFile sourceFile)
        : base(sourceFile)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ZipArchiveFolder"/>.
    /// </summary>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="sourceData">The Id and Name of the "source file" that created this item.</param>
    public ZipArchiveFolder(ZipArchive archive, SimpleStorableItem sourceData)
        : base(archive, sourceData)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ZipArchiveFolder"/>.
    /// </summary>
    /// <remarks>
    /// This constructor is used internally for subfolders.
    /// </remarks>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="name">The name of this item</param>
    /// <param name="parent">The parent of this folder.</param>
    internal ZipArchiveFolder(ZipArchive archive, string name, ReadOnlyZipArchiveFolder parent)
        : base(archive, name, parent)
    {
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await OpenArchiveAsync(cancellationToken);

        using var srcStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (srcStream.CanSeek && srcStream.Position != 0)
            srcStream.Seek(0, SeekOrigin.Begin);
        else if (srcStream.Position != 0)
            throw new InvalidOperationException("The opened file stream is not at position 0 and cannot seek. Unable to copy.");

        var existingEntry = TryGetEntry($"{Path}{fileToCopy.Name}");
        if (!overwrite && existingEntry is not null)
            return new ZipArchiveEntryFile(existingEntry, this);

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
        Archive ??= await OpenArchiveAsync(cancellationToken);

        var realSubPath = $"{Path}{name}";

        var entry = TryGetEntry(realSubPath);
        if (overwrite && entry is not null)
        {
            entry.Delete();
            entry = null;
        }
        entry ??= Archive.CreateEntry(realSubPath);

        return new ZipArchiveEntryFile(entry, this);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Archive ??= await OpenArchiveAsync(cancellationToken);

        var subPath = $"{Path}{name}{ZIP_DIRECTORY_SEPARATOR}";
        GetVirtualFolders().TryGetValue(subPath, out var folder);

        if (overwrite && folder is not null)
        {
            await DeleteAsync(folder, cancellationToken);
            folder = null;
        }

        if (folder is null)
        {
            Archive ??= await OpenArchiveAsync(cancellationToken);
            folder = new ZipArchiveFolder(Archive, name, this);
            GetVirtualFolders()[subPath] = folder;
        }

        return folder;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Archive ??= await OpenArchiveAsync(cancellationToken);

        if (item is ZipArchiveFolder folder)
        {
            // Recursively remove any sub-entries
            // Pre-enumerate because the entries list will change in the loop
            var childEntries = Archive.Entries
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
    }

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolderWatcher>(new ZipArchiveFolderWatcher(this));
    }

    /// <inheritdoc/>
    public async Task<IAddressableStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IAddressableStorable item;

        Archive ??= await OpenArchiveAsync(cancellationToken);

        // Id can act as a Path (matches entry names) if we remove the prepended root folder Id.
        id = id.Replace(RootFolder.Id.TrimEnd(ZIP_DIRECTORY_SEPARATOR), string.Empty);

        var entry = TryGetEntry(id);
        if (entry is not null)
        {
            // Get file
            item = new ZipArchiveEntryFile(entry, this);
        }
        else
        {
            // Get folder
            item = GetVirtualFolders()[id];
        }

        return item;
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

    /// <summary>
    /// Manually opens the <see cref="Archive"/>.
    /// </summary>
    /// <returns>The opened archive. Dispose of it when you're done.</returns>
    public override async Task<ZipArchive> OpenArchiveAsync(CancellationToken cancellationToken = default)
    {
        if (Archive is not null)
            return Archive;

        if (SourceFile is null)
            throw new ArgumentNullException(nameof(SourceFile));

        var stream = await SourceFile.OpenStreamAsync(FileAccess.ReadWrite, cancellationToken);
        return Archive = new ZipArchive(stream, ZipArchiveMode.Update);
    }
}
