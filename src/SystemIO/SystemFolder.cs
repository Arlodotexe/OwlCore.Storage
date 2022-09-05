using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace OwlCore.Storage.SystemIO;

/// <summary>
/// An <see cref="IFolder"/> implementation that uses System.IO.
/// </summary>
public class SystemFolder : IModifiableFolder, IAddressableFolder, IFolderCanFastGetItem
{
    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="path">The path to the folder.</param>
    public SystemFolder(string path)
    {
        // For consistency, always remove the trailing directory separator.
        path = path.TrimEnd(System.IO.Path.PathSeparator, System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        if (!Directory.Exists(path))
            throw new FileNotFoundException($"Directory not found at path {path}");

        Id = path;
        Name = System.IO.Path.GetFileName(path) ?? throw new ArgumentException($"Could not determine directory name from path {path}");
        Path = path;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="directoryInfo">The directory to use.</param>
    public SystemFolder(DirectoryInfo directoryInfo) 
        : this(directoryInfo.FullName)
    {
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc />
    public async IAsyncEnumerable<IStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        if (type.HasFlag(StorableType.All))
        {
            foreach (var item in Directory.EnumerateFileSystemEntries(Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item is null)
                    continue;

                if (IsFolder(item))
                    yield return new SystemFolder(item);

                else if (IsFile(item))
                    yield return new SystemFile(item);
            }

            yield break;
        }

        if (type.HasFlag(StorableType.File))
        {
            foreach (var file in Directory.EnumerateFiles(Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (file is null)
                    continue;

                yield return new SystemFile(file);
            }
        }

        if (type.HasFlag(StorableType.Folder))
        {
            foreach (var folder in Directory.EnumerateDirectories(Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (folder is null)
                    continue;

                yield return new SystemFolder(folder);
            }
        }
    }

    /// <inheritdoc />
    public Task<IStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        // Since the path is used as the id, we can provide a fast method of getting a single item, without iterating.
        if (IsFile(id))
        {
            // Capture file name, combine with known path. Forces reading from current folder.
            var fileName = System.IO.Path.GetFileName(id) ?? throw new ArgumentException($"Could not determine file name from id: {id}");
            var fullPath = System.IO.Path.Combine(Path, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"The provided ID does not belong to an item in this folder.");

            return Task.FromResult<IStorable>(new SystemFile(fullPath));
        }

        if (IsFolder(id))
        {
            // Ensure containing directory matches current folder.
            var containingDirectory = System.IO.Path.GetDirectoryName(id);

            if (containingDirectory != Path || !Directory.Exists(id))
                throw new FileNotFoundException($"The provided ID does not belong to an item in this folder.");

            return Task.FromResult<IStorable>(new SystemFile(id));
        }

        throw new ArgumentException($"Could not determine if the provided path is a file or folder. Path: {id}");
    }

    /// <inheritdoc />
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolderWatcher>(new SystemFolderWatcher(this));
    }

    /// <inheritdoc />
    public Task DeleteAsync(IStorable item, CancellationToken cancellationToken = default)
    {
        if (IsFolder(item.Id))
            Directory.Delete(item.Id);

        if (IsFile(item.Id))
            File.Delete(item.Id);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, fileToCopy.Name);

        // Use provided system methods where possible.
        if (fileToCopy is SystemFile sysFile)
        {
            File.Copy(sysFile.Path, newPath, overwrite);
            return new SystemFile(newPath);
        }

        // Manual file copy. Slower, but covers all other scenarios.
        using var sourceStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);

        if (sourceStream.CanSeek)
            sourceStream.Seek(0, SeekOrigin.Begin);

        using var destinationStream = File.Create(newPath);

        await sourceStream.CopyToAsync(destinationStream, bufferSize: 81920, cancellationToken);

        return new SystemFile(newPath);
    }

    /// <inheritdoc />
    public async Task<IFile> MoveFromAsync(IFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, fileToMove.Name);

        // Use provided system methods where possible.
        if (fileToMove is SystemFile sysFile)
        {
            // In all .NET versions, you can call Delete(String) before calling Move, which will only delete the file if it exists.
            if (overwrite)
                File.Delete(newPath);

            File.Move(sysFile.Path, newPath);

            return new SystemFile(newPath);
        }
        
        // Manual move. Slower, but covers all other scenarios.
        var file = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }

    /// <inheritdoc />
    public Task<IFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, name);

        // In all .NET versions, you can call Delete(String) before calling Move, which will only delete the file if it exists.
        if (overwrite)
            File.Delete(newPath);

        Directory.CreateDirectory(newPath);
        return Task.FromResult<IFolder>(new SystemFolder(newPath));
    }

    /// <inheritdoc />
    public Task<IFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, name);

        // In all .NET versions, you can call Delete(String) before calling Move, which will only delete the file if it exists.
        if (overwrite)
            File.Delete(newPath);

        File.Create(newPath).Dispose();
        return Task.FromResult<IFile>(new SystemFile(newPath));
    }

    /// <inheritdoc />
    public Task<IAddressableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IAddressableFolder?>(Directory.GetParent(Path) is { } di ? new SystemFolder(di) : null);
    }

    private static bool IsFile(string path) => System.IO.Path.GetFileName(path) is { } str && str != string.Empty && File.Exists(path);

    private static bool IsFolder(string path) => Directory.Exists(path);
}