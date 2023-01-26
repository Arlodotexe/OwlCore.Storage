using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace OwlCore.Storage.Archive;

/// <summary>
/// A folder implementation wrapping a <see cref="ZipArchive"/> with
/// mode <see cref="ZipArchiveMode.Read"/> or <see cref="ZipArchiveMode.Update"/>.
/// </summary>
public class ReadOnlyZipFolder : IAddressableFolder
{
    /// <summary>
    /// The directory separator as defined by the ZIP standard.
    /// This is constant no matter the operating system (see 4.4.17.1).
    /// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
    /// </summary>
    protected const char ZIP_DIRECTORY_SEPARATOR = '/';
    
    protected readonly Stream _zipStream;
    private readonly ZipArchiveMode _mode;
    protected readonly Dictionary<string, ZipFolder> _virtualFolders = new();
    
    private ZipArchive? _archive;
    
    /// <summary>
    /// Creates a new instance of <see cref="ZipFolder"/>.
    /// </summary>
    /// <param name="zipStream">The stream containing the ZIP archive.</param>
    /// <param name="storable">A storable containing the ID and name to use for this folder.</param>
    /// <param name="path">The relative path inside the ZIP archive. Leave empty for the root folder.</param>
    public ReadOnlyZipFolder(Stream zipStream, IStorable storable, string path = "")
    {
        _zipStream = zipStream;
        
        Id = storable.Id;
        Name = storable.Name;
        Path = NormalizeEnding(path);
    }

    protected ReadOnlyZipFolder(Stream zipStream, string rootId, string path, ZipArchiveMode mode = ZipArchiveMode.Read)
        : this(zipStream, new SimpleStorableItem($"{rootId}{ZIP_DIRECTORY_SEPARATOR}{path}", IOPath.GetFileName(path)), path)
    {
        _mode = mode;
    }
    
    /// <inheritdoc/>
    public string Id { get; }
    
    /// <inheritdoc/>
    public string Name { get; }
    
    /// <inheritdoc/>
    public string Path { get; }
    
    /// <inheritdoc/>
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        if (type.HasFlag(StorableType.File))
        {
            foreach (var entry in GetArchive().Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsChild(entry.FullName))
                    yield return new ZipEntryFile(entry, this);
            }
        }

        if (type.HasFlag(StorableType.Folder))
        {
            foreach (var virtualFolder in _virtualFolders.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string pathWithoutTrailingSep = virtualFolder.Path.Substring(0, virtualFolder.Path.Length - 1);
                if (IsChild(pathWithoutTrailingSep))
                    yield return virtualFolder;
            }
        }
    }
    
    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Implement proper parent traversal
        return Task.FromResult<IFolder?>(null);
    }
    
    protected static string NormalizeEnding(string path)
    {
        return path.Length == 0 || path[path.Length - 1] == ZIP_DIRECTORY_SEPARATOR
            ? path
            : path + ZIP_DIRECTORY_SEPARATOR;
    }

    protected ZipArchiveEntry? TryGetEntry(string entryName)
    {
        return GetArchive().Mode != ZipArchiveMode.Create
            ? GetArchive().GetEntry(entryName)
            : null;
    }

    protected ZipArchive GetArchive()
    {
        if (_archive is null)
        {
            // Lazy init the archive
            _archive = new ZipArchive(_zipStream, _mode);
            
            // Populate list of virtual folders
            foreach (var entry in _archive.Entries)
            {
                string path = NormalizeEnding(entry.FullName);
                if (IsChild(path))
                    _virtualFolders[path] = new ReadOnlyZipFolder();
            }
        }
        
        return _archive;
    }
    
    protected bool IsChild(string path)
    {
        int idx = path.IndexOf(Path, StringComparison.Ordinal);
        if (idx != 0)
            return false;
            
        // The folder path is the start of the item path,
        // which means this item is at least a descendant
        // of this folder.

        // If there are no more directory separators after
        // the matched path, the item is a direct child.
        idx = path.IndexOf(ZIP_DIRECTORY_SEPARATOR, Path.Length + 1);
        return idx < 0;
    }
    
    protected virtual IFolder CreateSubfolder(Stream stream, string path, )
}