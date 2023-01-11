using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using IOPath = System.IO.Path;

#pragma warning disable CS1998

namespace OwlCore.Storage.SystemIO.Compression;

/// <summary>
/// A folder implementation wrapping a <see cref="ZipArchive"/>.
/// </summary>
public class ZipArchiveFolder : IAddressableFolder, IModifiableFolder, IFolderCanFastGetItem
{
    private readonly ZipArchive _archive;
    private readonly string _realPath;

    /// <summary>
    /// Creates a new instance of <see cref="ZipArchiveFolder"/>.
    /// </summary>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    /// <param name="stream">An existing stream which is provided as contents of the ZIP archive.</param>
    public ZipArchiveFolder(string id, string name, Stream stream) : this(id, name, new ZipArchive(stream))
    {

    }

    /// <summary>
    /// Creates a new instance of <see cref="ZipArchiveFolder"/>.
    /// </summary>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    /// <param name="archive">An existing ZIP archive which is provided as contents of the folder.</param>
    /// <param name="id">The id of the virtual folder, or <see cref="string.Empty"/> if root.</param>
    public ZipArchiveFolder(string id, string name, ZipArchive archive)
    {
        Id = Path = id;
        Name = name;

        _archive = archive;

        // This is the ID inside the archive,
        // the same as the 'path' but without the
        // name of the root folder.
        _realPath = IOPath.GetPathRoot(Path);
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var copyEntry = _archive.CreateEntry(IOPath.Combine(_realPath, fileToCopy.Name));

        using var srcStream = await fileToCopy.OpenStreamAsync();
        using var dstStream = copyEntry.Open();
        await srcStream.CopyToAsync(dstStream);

        return new ZipArchiveEntryFile(copyEntry, this);
    }

    /// <inheritdoc/>
    public Task<IAddressableFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = _archive.CreateEntry(IOPath.Combine(_realPath, name));
        return Task.FromResult<IAddressableFile>(new ZipArchiveEntryFile(entry, this));
    }

    /// <inheritdoc/>
    public Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string subPath = IOPath.Combine(Path, name);
        return Task.FromResult<IAddressableFolder>(new ZipArchiveFolder(subPath, name, _archive));
    }

    /// <inheritdoc/>
    public Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = _archive.GetEntry(item.Path);
        entry?.Delete();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolderWatcher>(new ZipArchiveFolderWatcher(this));
    }

    /// <inheritdoc/>
    public Task<IAddressableStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = _archive.GetEntry(id);

        IAddressableStorable item = entry is not null
            ? new ZipArchiveEntryFile(entry, this)
            : new ZipArchiveFolder(id, IOPath.GetFileName(id), _archive);

        return Task.FromResult(item);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        if (type.HasFlag(StorableType.File))
        {
            foreach (var entry in _archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new ZipArchiveEntryFile(entry, this);
            }
        }
    }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolder?>(null);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> MoveFromAsync(IAddressableFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Zip archives can't move files around, so we have to take
        // the slower manual id every time.
        var file = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }
}
