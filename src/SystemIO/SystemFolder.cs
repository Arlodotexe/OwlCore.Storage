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
public class SystemFolder : IModifiableFolder, IAddressableFolder, IFolderCanFastGetItem, IFolderCanFastGetFirstItemByName
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
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
    public Task<IAddressableStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        // Since the path is used as the id, we can provide a fast method of getting a single item, without iterating.
        if (IsFile(id))
        {
            // Capture file name, combine with known path. Forces reading from current folder.
            var fileName = System.IO.Path.GetFileName(id) ?? throw new ArgumentException($"Could not determine file name from id: {id}");
            var fullPath = System.IO.Path.Combine(Path, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"The provided ID does not belong to an item in this folder.");

            return Task.FromResult<IAddressableStorable>(new SystemFile(fullPath));
        }

        if (IsFolder(id))
        {
            // Ensure containing directory matches current folder.
            if (System.IO.Path.GetDirectoryName(id) != Path || !Directory.Exists(id))
                throw new FileNotFoundException($"The provided ID does not belong to an item in this folder.");

            return Task.FromResult<IAddressableStorable>(new SystemFile(id));
        }

        throw new ArgumentException($"Could not determine if the provided path is a file or folder. Path: {id}");
    }

    /// <inheritdoc/>
    public async Task<IAddressableStorable> GetFirstItemByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync(System.IO.Path.Combine(Path, name), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolderWatcher>(new SystemFolderWatcher(this));
    }

    /// <inheritdoc />
    public Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
    {
        // Ensure containing directory matches current folder.
        if (GetParentPath(item.Path).TrimEnd(System.IO.Path.DirectorySeparatorChar) != Path)
            throw new FileNotFoundException($"The provided item does not exist in this folder.");

        if (IsFolder(item.Path))
            Directory.Delete(item.Path, recursive: true);

        if (IsFile(item.Path))
            File.Delete(item.Path);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, fileToCopy.Name);

        // Use provided system methods where possible.
        if (fileToCopy is SystemFile sysFile)
        {
            // If the target and destination are the same, there's no need to copy.
            if (sysFile.Path == newPath)
                return new SystemFile(newPath);

            if (File.Exists(newPath))
            {
                if (!overwrite)
                    return new SystemFile(newPath);

                File.Delete(newPath);
            }
            
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
    public async Task<IAddressableFile> MoveFromAsync(IAddressableFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, fileToMove.Name);

        // Use provided system methods where possible.
        if (fileToMove is SystemFile sysFile)
        {
            if (File.Exists(newPath) && !overwrite)
                return new SystemFile(newPath);

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
    public Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, name);

        if (overwrite)
            Directory.Delete(newPath, recursive: true);

        Directory.CreateDirectory(newPath);
        return Task.FromResult<IAddressableFolder>(new SystemFolder(newPath));
    }

    /// <inheritdoc />
    public Task<IAddressableFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = System.IO.Path.Combine(Path, name);

        if (overwrite || !File.Exists(newPath))
            File.Create(newPath).Dispose();

        return Task.FromResult<IAddressableFile>(new SystemFile(newPath));
    }

    /// <inheritdoc />
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(Directory.GetParent(Path) is { } di ? new SystemFolder(di) : null);
    }

    private static bool IsFile(string path) => System.IO.Path.GetFileName(path) is { } str && str != string.Empty && File.Exists(path);
    private static bool IsFolder(string path) => Directory.Exists(path);

    string GetParentPath(string relativePath)
    {
        // Path.GetDirectoryName() treats strings that end with a directory separator as a directory. If there's no trailing slash, it's treated as a file.
        // Run it twice for folders. The first time only shaves off the trailing directory separator.
        var parentDirectoryName = relativePath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) ? System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(relativePath)) : System.IO.Path.GetDirectoryName(relativePath);

        // It also doesn't return a string that has a path separator at the end.
        return parentDirectoryName + System.IO.Path.DirectorySeparatorChar;
    }

    string GetParentDirectoryName(string relativePath)
    {
        var parentPath = GetParentPath(relativePath);
        var parentParentPath = GetParentPath(parentPath);

        return parentPath.Replace(parentParentPath, "").TrimEnd(System.IO.Path.DirectorySeparatorChar);
    }
}