using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO.Compression;

/// <summary>
/// A folder implementation wrapping a <see cref="ZipArchive"/>.
/// </summary>
public class ZipArchiveFolder : ReadOnlyZipArchiveFolder, IModifiableFolder
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
    public async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
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
    public async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
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
    public async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Archive ??= await OpenArchiveAsync(cancellationToken);

        if (item is ZipArchiveFolder folder)
        {
            var itemEntryId = folder.Id.Replace(Id, "");

            if (!GetVirtualFolders().ContainsKey(itemEntryId))
                throw new FileNotFoundException("The item was not found in the folder.");

            // Recursively remove any sub-entries
            // Pre-enumerate because the entries list will change in the loop
            var childEntries = Archive.Entries
                .Where(e => IsChild(e.FullName, folder.Path))
                .ToList();

            foreach (var entry in childEntries)
                entry.Delete();

            GetVirtualFolders().Remove(itemEntryId);
        }
        else if (item is ReadOnlyZipArchiveFolder readOnlyFolder)
        {
            var entry = TryGetEntry(readOnlyFolder.Path);

            if (entry is null)
                throw new FileNotFoundException("The item was not found in the folder.");
            else
                entry.Delete();
        }
    }

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolderWatcher>(new ZipArchiveFolderWatcher(this));
    }

    /// <summary>
    /// Manually opens the archive for this folder.
    /// </summary>
    /// <returns>The opened archive.</returns>
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
