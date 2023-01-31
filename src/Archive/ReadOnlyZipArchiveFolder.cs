using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace OwlCore.Storage.Archive;

/// <summary>
/// A folder implementation wrapping a <see cref="ZipArchive"/> with
/// mode <see cref="ZipArchiveMode.Read"/> or <see cref="ZipArchiveMode.Update"/>.
/// </summary>
public class ReadOnlyZipArchiveFolder : IAddressableFolder, IDisposable
{
    /// <summary>
    /// The directory separator as defined by the ZIP standard.
    /// This is constant no matter the operating system (see 4.4.17.1).
    /// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
    /// </summary>
    internal const char ZIP_DIRECTORY_SEPARATOR = '/';

    private readonly IFolder? _parent;
    private protected Dictionary<string, IAddressableFolder>? _virtualFolders;

    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyZipArchiveFolder"/>.
    /// </summary>
    /// <param name="sourceFile">The file that this archive originated from.</param>
    public ReadOnlyZipArchiveFolder(IFile sourceFile)
        : this((IStorable)sourceFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile.Id))
            throw new ArgumentNullException(nameof(sourceFile.Id), "Source file's ID cannot be null or empty.");

        SourceFile = sourceFile;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyZipArchiveFolder"/>.
    /// </summary>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="sourceData">Data representing the file that this archive originated from.</param>
    public ReadOnlyZipArchiveFolder(ZipArchive archive, SimpleStorableItem sourceData)
        : this(sourceData)
    {
        Archive = archive;
    }

    private ReadOnlyZipArchiveFolder(IStorable sourceData)
    {
        if (string.IsNullOrWhiteSpace(sourceData.Id))
            throw new ArgumentNullException(nameof(sourceData.Id), "Source file's ID cannot be null or empty.");

        RootFolder = this;

        // e.g., a file named MyArchive.zip becomes a folder named MyArchive
        Name = IOPath.GetFileNameWithoutExtension(sourceData.Name);

        Id = $"{sourceData.Id.Trim(ZIP_DIRECTORY_SEPARATOR)}{ZIP_DIRECTORY_SEPARATOR}";
        Path = string.Empty;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyZipArchiveFolder"/>.
    /// </summary>
    /// <param name="archive">An existing ZIP archive which is provided as the contents of the folder.</param>
    /// <param name="name">The name of this item</param>
    /// <param name="parent">The parent of this folder.</param>
    internal ReadOnlyZipArchiveFolder(ZipArchive archive, string name, ReadOnlyZipArchiveFolder parent)
    {
        Archive = archive;
        _parent = parent;

        RootFolder = parent.RootFolder;
        SourceFile = parent.SourceFile;

        Name = name;
        Path = $"{parent.Path}{name}{ZIP_DIRECTORY_SEPARATOR}";
        Id = $"{parent.Id}{name}{ZIP_DIRECTORY_SEPARATOR}";
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Path { get; }

    /// <summary>
    /// A folder that represents the root of the archive.
    /// </summary>
    public IFolder RootFolder { get; }

    /// <summary>
    /// The file that this archive originated from.
    /// </summary>
    public IFile? SourceFile { get; }

    /// <summary>
    /// The instance used to explore the archive.
    /// </summary>
    public ZipArchive? Archive { get; protected set; }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Archive ??= await OpenArchiveAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        if (type.HasFlag(StorableType.File))
        {
            Archive ??= await OpenArchiveAsync(cancellationToken);
            foreach (var entry in Archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsChild(entry.FullName))
                    yield return new ZipArchiveEntryFile(entry, this);
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
        return Task.FromResult(_parent);
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
        if (Archive is null)
            throw new ArgumentNullException(nameof(Archive));

        return Archive.Mode != ZipArchiveMode.Create
            ? (Archive.GetEntry(entryName) ?? Archive.GetEntry(entryName.TrimEnd(ZIP_DIRECTORY_SEPARATOR)))
            : null;
    }

    /// <summary>
    /// Gets the list of virtual folders.
    /// </summary>
    protected Dictionary<string, IAddressableFolder> GetVirtualFolders()
    {
        if (Archive is null)
            throw new ArgumentNullException(nameof(Archive));

        if (_virtualFolders is null)
        {
            _virtualFolders = new();

            // Populate list of virtual folders
            var entryPaths = Archive.Entries
                .Select(e => NormalizeEnding(e.FullName))
                .ToList();

            for (var e = 0; e < Archive.Entries.Count; e++)
            {
                var path = entryPaths[e];
                if (!IsChild(path) || entryPaths.Any(p => p.StartsWith(path)))
                    continue;

                var entry = Archive.Entries[e];
                _virtualFolders[path] = new ReadOnlyZipArchiveFolder(Archive, entry.Name, this);
            }
        }

        return _virtualFolders;
    }

    /// <summary>
    /// Determines if the <paramref name="path"/> is a child of the given or current folder.
    /// </summary>
    /// <param name="path">The child path to check.</param>
    /// <param name="parentPath">The potential parent path.</param>
    protected bool IsChild(string path, string? parentPath = null)
    {
        parentPath ??= Path;

        // Remove trailing separator
        path = path.TrimEnd(ZIP_DIRECTORY_SEPARATOR);

        var idx = path.IndexOf(parentPath, StringComparison.Ordinal);
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
    /// Ensures the given path ends with exactly one directory separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized string with a trailing directory separator.</returns>
    public static string NormalizeEnding(string path)
    {
        return path.Length == 0 || path[path.Length - 1] == ZIP_DIRECTORY_SEPARATOR
            ? path
            : path + ZIP_DIRECTORY_SEPARATOR;
    }

    /// <summary>
    /// Manually opens the <see cref="Archive"/>.
    /// </summary>
    /// <returns>The opened archive. Dispose of it when you're done.</returns>
    public virtual async Task<ZipArchive> OpenArchiveAsync(CancellationToken cancellationToken = default)
    {
        if (Archive is not null)
            throw new ArgumentException(nameof(Archive), $"Argument was expected to be null, got {Archive}");

        if (SourceFile is null)
            throw new ArgumentNullException(nameof(SourceFile));

        var stream = await SourceFile.OpenStreamAsync(FileAccess.Read, cancellationToken);
        return Archive = new ZipArchive(stream, ZipArchiveMode.Read);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Archive?.Dispose();
        Archive = null;
    }
}