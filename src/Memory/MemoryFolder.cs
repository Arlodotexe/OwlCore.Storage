using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>
/// A folder implementation that resides in memory.
/// </summary>
public class MemoryFolder : IModifiableFolder, IChildFolder, IGetItem
{
    private readonly MemoryFolderWatcher _folderWatcher;

    /// <summary>
    /// Creates a new instance of <see cref="MemoryFolder"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    public MemoryFolder(string id, string name)
    {
        Id = id;
        Name = name;

        _folderWatcher = new MemoryFolderWatcher(this);
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
    public virtual IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        return FolderContents.Values.Where(x =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return x is IFile && type.HasFlag(StorableType.File) ||
                   x is IFolder && type.HasFlag(StorableType.Folder);
        }).ToAsyncEnumerable();
    }

    /// <inheritdoc />
    public virtual Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IFolderWatcher>(_folderWatcher);
    }

    /// <inheritdoc />
    public virtual Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!FolderContents.TryGetValue(id, out var content))
            throw new FileNotFoundException();

        return Task.FromResult(content);
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!FolderContents.ContainsKey(item.Id))
            throw new FileNotFoundException();

        FolderContents.Remove(item.Id);
        _folderWatcher.NotifyItemRemoved(new SimpleStorableItem(item.Id, item.Name));

        return Task.CompletedTask;
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
            FolderContents.Add(file.Id, file);
        else
            FolderContents[file.Id] = file;

        _folderWatcher.NotifyItemAdded(file);

        return file;
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(Parent);
    }
}