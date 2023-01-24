﻿using System;
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
public class ZipFolder : IAddressableFolder, IModifiableFolder, IFolderCanFastGetItem
{
    /// <summary>
    /// The directory separator as defined by the ZIP standard.
    /// This is constant no matter the operating system (see 4.4.17.1).
    /// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
    /// </summary>
    private const char ZIP_DIRECTORY_SEPARATOR = '/';

    private readonly Stream _stream;
    private readonly IStorable _rootStorable;
    private readonly Dictionary<string, ZipFolder> _virtualFolders = new();
    
    private ZipArchive? _archive;

    /// <summary>
    /// Creates a new instance of <see cref="ZipFolder"/>.
    /// </summary>
    /// <param name="stream">The stream containing the ZIP archive.</param>
    /// <param name="storable">A storable containing the ID and name to use for the root folder.</param>
    /// <param name="path">The relative path inside the ZIP archive. Leave empty for the root folder.</param>
    public ZipFolder(Stream stream, IStorable storable, string path = "")
    {
        Id = $"{storable.Id}_0782efd61b7a6b02e602cc6a11673ec9{ZIP_DIRECTORY_SEPARATOR}" + path;
        Name = System.IO.Path.GetFileNameWithoutExtension(storable.Name);
        Path = NormalizeEnding(path);

        _stream = stream;
        _rootStorable = storable;
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
            folder = new ZipFolder(name, name, _archive, subPath);
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
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        bool IsChild(string path)
        {
            int idx = path.IndexOf(Path);
            if (idx == 0)
            {
                // The folder path is the start of the item path,
                // which means this item is at least a descendant
                // of this folder.

                // If there are no more directory separators after
                // the matched path, the item is a direct child.
                idx = path.IndexOf(ZIP_DIRECTORY_SEPARATOR, Path.Length + 1);
                return idx < 0;
            }

            return false;
        }

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

        return Task.FromResult<IFolder?>(null);
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

    private static string NormalizeEnding(string path)
    {
        return path.Length == 0 || path[path.Length - 1] == ZIP_DIRECTORY_SEPARATOR
            ? path
            : path + ZIP_DIRECTORY_SEPARATOR;
    }

    private ZipArchiveEntry? TryGetEntry(string entryName)
    {
        return GetArchive().Mode != ZipArchiveMode.Create
            ? GetArchive().GetEntry(entryName)
            : null;
    }

    private ZipArchive GetArchive()
    {
        _archive ??= new ZipArchive(_stream, ZipArchiveMode.Update);
        return _archive;
    }
}