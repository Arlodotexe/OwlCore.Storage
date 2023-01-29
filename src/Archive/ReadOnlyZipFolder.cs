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
    internal const char ZIP_DIRECTORY_SEPARATOR = '/';
    
    protected readonly ZipArchive _archive;
    protected readonly IFolder? _parent;
    
    protected Dictionary<string, IAddressableFolder>? _virtualFolders;
    
    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyZipFolder"/>.
    /// </summary>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="storable">A storable containing the ID, name, and path of this folder.</param>
    /// <param name="parent">The parent of this folder, if one exists.</param>
    public ReadOnlyZipFolder(ZipArchive archive, SimpleZipStorableItem storable, IFolder? parent = null)
    {
        _archive = archive;
        _parent = parent;
        
        Id = storable.Id;
        Name = storable.Name;
        Path = storable.Path;
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
            foreach (var entry in _archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsChild(entry.FullName))
                    yield return new ZipEntryFile(entry, this);
            }
        }

        if (type.HasFlag(StorableType.Folder))
        {
            foreach (var virtualFolder in GetVirtualFolders().Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsChild(virtualFolder.Path))
                    yield return virtualFolder;
            }
        }
    }
    
    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IFolder?>(_parent);
    }

    /// <summary>
    /// Attempts to get the entry, without throwing if the archive does not support reading entries. 
    /// </summary>
    /// <param name="entryName">The name of the entry to get.</param>
    /// <returns>
    /// The matching <see cref="ZipArchiveEntry"/> if one exists, or
    /// <see langword="null"/> if the archive does not support reading or
    /// the entry does not exist.
    /// </returns>
    protected ZipArchiveEntry? TryGetEntry(string entryName)
    {
        return _archive.Mode != ZipArchiveMode.Create
            ? _archive.GetEntry(entryName)
            : null;
    }

    /// <summary>
    /// Gets the list of virtual folders.
    /// </summary>
    protected Dictionary<string, IAddressableFolder> GetVirtualFolders()
    {
        if (_virtualFolders is null)
        {
            _virtualFolders = new();
        
            // Populate list of virtual folders
            foreach (var entry in _archive.Entries)
            {
                string path = SimpleZipStorableItem.NormalizeEnding(entry.FullName);
                if (IsChild(path))
                    _virtualFolders[path] = CreateSubfolderItem(_archive, new SimpleZipStorableItem(Id, entry), this);
            }    
        }
        
        return _virtualFolders;
    }
    
    /// <summary>
    /// Determines if the <paramref name="path"/> is a child of the current folder.
    /// </summary>
    protected bool IsChild(string path)
    {
        // Remove trailing separator
        if (path[path.Length - 1] == ZIP_DIRECTORY_SEPARATOR)
            path = path.Substring(0, path.Length - 1);
        
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

    /// <summary>
    /// Creates a new <see cref="IFolder"/> object with the current folder as its parent.
    /// </summary>
    /// <param name="archive">The ZIP archive.</param>
    /// <param name="storable">The ID, name, and path of the new folder.</param>
    /// <param name="parent">The parent of the folder, if one exists..</param>
    /// <returns></returns>
    protected virtual IAddressableFolder CreateSubfolderItem(ZipArchive archive, SimpleZipStorableItem storable, IFolder? parent = null)
    {
        return new ReadOnlyZipFolder(archive, storable, parent);
    }
}