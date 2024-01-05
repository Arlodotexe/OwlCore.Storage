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
public class MemoryFolder : IModifiableFolder, IChildFolder, IFastGetItem
{
    private readonly Dictionary<string, IStorableChild> _folderContents = new();
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
    public MemoryFolder? Parent { get; internal set; }

    /// <inheritdoc />
    public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        return _folderContents.Values.Where(x =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return x is IFile && type.HasFlag(StorableType.File) ||
                   x is IFolder && type.HasFlag(StorableType.Folder);
        }).ToAsyncEnumerable();
    }

    /// <inheritdoc />
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IFolderWatcher>(_folderWatcher);
    }

    /// <inheritdoc />
    public Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_folderContents.ContainsKey(id))
            throw new FileNotFoundException();

        return Task.FromResult(_folderContents[id]);
    }

    /// <inheritdoc />
    public Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_folderContents.ContainsKey(item.Id))
            throw new FileNotFoundException();

        _folderContents.Remove(item.Id);
        _folderWatcher.NotifyItemRemoved(new SimpleStorableItem(item.Id, item.Name));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        else if (stream.Position == 0)
        {
            throw new InvalidOperationException("The opened file stream is not at position 0 and cannot be seeked. Unable to copy.");
        }

        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var file = new MemoryFile(fileToCopy.Id, fileToCopy.Name, memoryStream)
        {
            Parent = this,
        };

        if (!overwrite && _folderContents.TryGetValue(file.Id, out var existingStorable) && existingStorable is IChildFile existingFile)
            return existingFile;

        if (!_folderContents.ContainsKey(file.Id))
            _folderContents.Add(file.Id, file);
        else
            _folderContents[file.Id] = file;

        _folderWatcher.NotifyItemAdded(file);

        return file;
    }

    /// <inheritdoc />
    public async Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Manual move. Slower, but covers all scenarios.
        var file = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);
        _folderWatcher.NotifyItemRemoved(new SimpleStorableItem(fileToMove.Id, fileToMove.Name));

        return file;
    }

    /// <inheritdoc />
    public async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingFolderKvp = _folderContents.FirstOrDefault(x => x.Value.Name == name && x.Value is IFolder);
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

        if (!_folderContents.ContainsKey(folder.Id))
        {
            _folderContents.Add(folder.Id, folder);
            _folderWatcher.NotifyItemAdded(folder);
        }
        else
            _folderContents[folder.Id] = folder;

        return folder;
    }

    /// <inheritdoc />
    public async Task<IChildFile> CreateFileAsync(string name, bool overwrite = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingFileKvp = _folderContents.FirstOrDefault(x => x.Value.Name == name);
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

        if (!_folderContents.ContainsKey(file.Id))
            _folderContents.Add(file.Id, file);
        else
            _folderContents[file.Id] = file;

        _folderWatcher.NotifyItemAdded(file);

        return file;
    }

    /// <inheritdoc />
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(Parent);
    }
}