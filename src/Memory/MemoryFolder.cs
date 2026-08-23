using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>
/// A folder implementation that resides in memory.
/// </summary>
public class MemoryFolder : IModifiableFolder, IChildFolder, IGetItem, ICreatedAtOffset, ILastModifiedAtOffset, ILastAccessedAtOffset
{
    private readonly MemoryFolderWatcher _folderWatcher;

    /// <summary>
    /// Creates a new instance of <see cref="MemoryFile"/> with same new GUID as ID and Name.
    /// </summary>
    public MemoryFolder() : this(Guid.NewGuid().ToString())
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="MemoryFile"/> with the given <param name="nameId"/> as both Name and Id.
    /// </summary>
    public MemoryFolder(string nameId) : this($"{nameId.GetHashCode()}", nameId)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="MemoryFolder"/>.
    /// </summary>
    /// <param name="id">The ID for this instance.</param>
    /// <param name="name">The name for this instance.</param>
    public MemoryFolder(string id, string name)
    {
        Id = id;
        Name = name;

        _folderWatcher = new MemoryFolderWatcher(this);

        CreatedAtOffset = new MemoryCreatedAtOffsetProperty(this, new() { Value = DateTimeOffset.Now });
        CreatedAt = new MemoryCreatedAtProperty(this, new() { Value = DateTime.Now });

        LastModifiedAt = new MemoryLastModifiedAtProperty(this, new() { Value = null });
        LastModifiedAtOffset = new MemoryLastModifiedAtOffsetProperty(this, new() { Value = null });

        LastAccessedAt = new MemoryLastAccessedAtProperty(this, new() { Value = null });
        LastAccessedAtOffset = new MemoryLastAccessedAtOffsetProperty(this, new() { Value = null });
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the parent folder, if any.
    /// </summary>
    public MemoryFolder? Parent { get; protected internal set; }

    /// <summary>
    /// Gets the contents of the folder as a dictionary with <see cref="IStorableChild"/> items associated with unique item IDs.
    /// </summary>
    protected Dictionary<string, IStorableChild> FolderContents { get; } = new();

    /// <inheritdoc />
    public ICreatedAtOffsetProperty CreatedAtOffset { get; init; }

    /// <inheritdoc />
    public ICreatedAtProperty CreatedAt { get; init; }

    /// <inheritdoc />
    public ILastModifiedAtOffsetProperty LastModifiedAtOffset { get; init; }

    /// <inheritdoc />
    public ILastModifiedAtProperty LastModifiedAt { get; init; }

    /// <inheritdoc />
    public ILastAccessedAtOffsetProperty LastAccessedAtOffset { get; init; }

    /// <inheritdoc />
    public ILastAccessedAtProperty LastAccessedAt { get; init; }

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        UpdateLastAccessed();

        foreach (var item in FolderContents.Values.Where(x =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return x is IFile && type.HasFlag(StorableType.File) ||
                   x is IFolder && type.HasFlag(StorableType.Folder);
        }))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public virtual Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IFolderWatcher>(_folderWatcher);
    }

    /// <inheritdoc />
    public virtual async Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!FolderContents.TryGetValue(id, out var content))
            throw new FileNotFoundException();

        UpdateLastAccessed();

        return content;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!FolderContents.ContainsKey(item.Id))
            throw new FileNotFoundException();

        FolderContents.Remove(item.Id);
        _folderWatcher.NotifyItemRemoved(new SimpleStorableItem(item.Id, item.Name));

        UpdateLastModified();
    }

    /// <inheritdoc />
    public virtual async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingFolderKvp = FolderContents.FirstOrDefault(x => x.Value.Name == name && x.Value is IFolder);
        var existingFolder = existingFolderKvp.Value as IChildFolder;

        if (overwrite && existingFolder is not null)
        {
            await DeleteAsync(existingFolder, cancellationToken);
        }

        var emptyMemoryFolder = new MemoryFolder($"{Guid.NewGuid()}", name)
        {
            Parent = this,
        };

        IChildFolder folder = overwrite ? emptyMemoryFolder : (existingFolder ?? emptyMemoryFolder);

        if (!FolderContents.ContainsKey(folder.Id))
        {
            FolderContents.Add(folder.Id, folder);
            _folderWatcher.NotifyItemAdded(folder);
            UpdateLastModified();
        }
        else
            FolderContents[folder.Id] = folder;

        return folder;
    }

    /// <inheritdoc />
    public virtual async Task<IChildFile> CreateFileAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingFileKvp = FolderContents.FirstOrDefault(x => x.Value.Name == name);
        IChildFile? existingFile = (IChildFile?)existingFileKvp.Value;

        if (overwrite && existingFile is not null)
        {
            await DeleteAsync(existingFile, cancellationToken);
        }

        var emptyMemoryFolder = new MemoryFile($"{Guid.NewGuid()}", name, new MemoryStream())
        {
            Parent = this,
        };

        var file = overwrite ? emptyMemoryFolder : (existingFile ?? emptyMemoryFolder);

        if (!FolderContents.ContainsKey(file.Id))
        {
            FolderContents.Add(file.Id, file);
            _folderWatcher.NotifyItemAdded(file);
            UpdateLastModified();
        }
        else
            FolderContents[file.Id] = file;


        return file;
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(Parent);
    }

    private void UpdateLastAccessed()
    {
        var now = DateTimeOffset.Now;
        ((MemoryLastAccessedAtProperty)LastAccessedAt).DateTimeValue.Value = now.LocalDateTime;
        ((MemoryLastAccessedAtOffsetProperty)LastAccessedAtOffset).DateTimeOffsetValue.Value = now;
    }

    private void UpdateLastModified()
    {
        var now = DateTimeOffset.Now;
        ((MemoryLastModifiedAtProperty)LastModifiedAt).DateTimeValue.Value = now.LocalDateTime;
        ((MemoryLastModifiedAtOffsetProperty)LastModifiedAtOffset).DateTimeOffsetValue.Value = now;
    }
}