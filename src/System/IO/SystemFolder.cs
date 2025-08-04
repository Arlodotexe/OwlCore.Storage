using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace OwlCore.Storage.System.IO;

/// <summary>
/// An <see cref="IFolder"/> implementation that uses System.IO.
/// </summary>
public class SystemFolder : IModifiableFolder, IChildFolder, ICreateCopyOf, IMoveFrom, IGetItem, IGetItemRecursive, IGetFirstByName, IGetRoot
{
    private string? _name;
    private DirectoryInfo? _info;

    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="path">The path to the folder.</param>
    public SystemFolder(string path)
    {
        foreach (var c in global::System.IO.Path.GetInvalidPathChars())
        {
            if (path.Contains(c))
                throw new FormatException($"Provided path contains invalid character '{c}'.");
        }

        if (!Directory.Exists(path))
            throw new FileNotFoundException($"Directory not found at path '{path}'.");

        // For consistency, always remove the trailing directory separator.
        Path = path.TrimEnd(global::System.IO.Path.PathSeparator, global::System.IO.Path.DirectorySeparatorChar, global::System.IO.Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="info">The directory to use.</param>
    public SystemFolder(DirectoryInfo info)
    {
        if (!info.Exists)
            throw new FileNotFoundException($"Directory not found at path '{info.FullName}'.");

        _info = info;

        // For consistency, always remove the trailing directory separator.
        Path = info.FullName.TrimEnd(global::System.IO.Path.PathSeparator, global::System.IO.Path.DirectorySeparatorChar, global::System.IO.Path.AltDirectorySeparatorChar);
        _name = info.Name;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>
    /// </summary>
    /// <remarks>
    /// NOTE: This constructor does not verify whether the directory
    /// actually exists beforehand. Do not use outside of enumeration
    /// or when it's known that the folder exists.
    /// </remarks>
    /// <param name="path">The path to the folder.</param>
    /// <param name="noValidation">
    /// A required value for this overload. No functional difference between provided values.
    /// </param>
    internal SystemFolder(string path, bool noValidation)
    {
        // For consistency, always remove the trailing directory separator.
        Path = path.TrimEnd(global::System.IO.Path.PathSeparator, global::System.IO.Path.DirectorySeparatorChar, global::System.IO.Path.AltDirectorySeparatorChar);
    }


    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <remarks>
    /// NOTE: This constructor does not verify whether the directory
    /// actually exists beforehand. Do not use outside of enumeration
    /// or when it's known that the folder exists.
    /// </remarks>
    /// <param name="info">The directory to use.</param>
    /// <param name="noValidation">
    /// A required value for this overload. No functional difference between provided values.
    /// </param>
    internal SystemFolder(DirectoryInfo info, bool noValidation)
    {
        _info = info;

        // For consistency, always remove the trailing directory separator.
        Path = info.FullName.TrimEnd(global::System.IO.Path.PathSeparator, global::System.IO.Path.DirectorySeparatorChar, global::System.IO.Path.AltDirectorySeparatorChar);
        _name = info.Name;
    }

    /// <summary>
    /// Gets the underlying <see cref="DirectoryInfo"/> for this folder.
    /// </summary>
    public DirectoryInfo Info => _info ??= new DirectoryInfo(Path);

    /// <inheritdoc />
    public string Id => Path;

    /// <inheritdoc />
    public string Name => _name ??= global::System.IO.Path.GetFileName(Path) ?? throw new ArgumentException($"Could not determine directory name from path '{Path}'.");

    /// <summary>
    /// Gets the path of the folder on disk.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        if (type.HasFlag(StorableType.All))
        {
            foreach (var item in Info.EnumerateFileSystemInfos())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item is null)
                    continue;

                if (item.Attributes.HasFlag(FileAttributes.Directory))
                    yield return new SystemFolder((DirectoryInfo)item, noValidation: true);
                else
                    yield return new SystemFile((FileInfo)item, noValidation: true);
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

                yield return new SystemFile(file, noValidation: true);
            }
        }

        if (type.HasFlag(StorableType.Folder))
        {
            foreach (var folder in Directory.EnumerateDirectories(Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (folder is null)
                    continue;

                yield return new SystemFolder(folder, noValidation: true);
            }
        }
    }

    /// <inheritdoc />
    public virtual Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!id.Contains(Path))
            throw new FileNotFoundException($"The provided Id does not belong to an item in this folder.");

        // Since the path is used as the id, we can provide a fast method of getting a single item, without iterating.
        if (IsFile(id))
            return Task.FromResult<IStorableChild>(new SystemFile(id, noValidation: true));

        if (IsFolder(id))
            return Task.FromResult<IStorableChild>(new SystemFolder(id, noValidation: true));

        throw new ArgumentException($"Could not determine if the provided path is a file or folder. Path '{id}'.");
    }

    /// <inheritdoc />
    public virtual Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!id.Contains(Path))
            throw new FileNotFoundException($"The provided Id does not belong to an item in this folder.");

        // Since the path is used as the id, we can provide a fast method of getting a single item, without iterating.
        if (IsFile(id))
        {
            // Capture file name, combine with known path. Forces reading from current folder only.
            var fileName = global::System.IO.Path.GetFileName(id) ?? throw new ArgumentException($"Could not determine file name from Id '{id}'.");
            var fullPath = global::System.IO.Path.Combine(Path, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"The provided Id does not belong to an item in this folder.");

            return Task.FromResult<IStorableChild>(new SystemFile(fullPath, noValidation: true));
        }

        if (IsFolder(id))
        {
            // Ensure containing directory matches current folder.
            if (global::System.IO.Path.GetDirectoryName(id) != Path || !Directory.Exists(id))
                throw new FileNotFoundException($"The provided Id does not belong to an item in this folder.");

            return Task.FromResult<IStorableChild>(new SystemFolder(id, noValidation: true));
        }

        throw new FileNotFoundException($"Could not determine if the provided path exists, or whether it's a file or folder. Id '{id}'.");
    }

    /// <inheritdoc/>
    public virtual Task<IStorableChild> GetFirstByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return GetItemAsync(global::System.IO.Path.Combine(Path, name), cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolderWatcher>(new SystemFolderWatcher(this));
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        // Ensure containing directory matches current folder.
        if (GetParentPath(item.Id).TrimEnd(global::System.IO.Path.DirectorySeparatorChar) != Path)
            throw new FileNotFoundException($"The provided item does not exist in this folder.");

        if (IsFolder(item.Id))
            Directory.Delete(item.Id, recursive: true);
        else if (IsFile(item.Id))
            File.Delete(item.Id);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite, CancellationToken cancellationToken, CreateCopyOfDelegate fallback)
    {
        // Check if the file is a SystemFile. If not, use the fallback.
        if (fileToCopy is not SystemFile systemFile)
            return await fallback(this, fileToCopy, overwrite, cancellationToken);

        // Handle using System.IO
        var newPath = global::System.IO.Path.Combine(Path, systemFile.Name);

        if (File.Exists(newPath))
        {
            if (!overwrite)
                throw new FileAlreadyExistsException(fileToCopy.Name);

            File.Delete(newPath);
        }

        // If the source and destination are the same, there's no need to copy.
        if (systemFile.Path == newPath)
            return systemFile;

        File.Copy(systemFile.Path, newPath, overwrite);

        return new SystemFile(newPath, noValidation: true);
    }

    /// <inheritdoc />
    public virtual async Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken, MoveFromDelegate fallback)
    {
        // Check if the file is a SystemFile. If not, use the fallback.
        if (fileToMove is not SystemFile systemFile)
            return await fallback(this, fileToMove, source, overwrite, cancellationToken);

        // Handle using System.IO
        var newPath = global::System.IO.Path.Combine(Path, systemFile.Name);
        if (File.Exists(newPath) && !overwrite)
            return new SystemFile(newPath, noValidation: true);

        if (overwrite)
            File.Delete(newPath);

        File.Move(systemFile.Path, newPath);

        return new SystemFile(newPath, noValidation: true);
    }

    /// <inheritdoc />
    public virtual Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = global::System.IO.Path.Combine(Path, name);

        try
        {
            if (overwrite)
                Directory.Delete(newPath, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // Ignored
        }

        Directory.CreateDirectory(newPath);
        return Task.FromResult<IChildFolder>(new SystemFolder(newPath));
    }

    /// <inheritdoc />
    public virtual Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var newPath = global::System.IO.Path.Combine(Path, name);

        if (overwrite || !File.Exists(newPath))
            File.Create(newPath).Dispose();

        return Task.FromResult<IChildFile>(new SystemFile(newPath, noValidation: true));
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(Directory.GetParent(Path) is { } di ? new SystemFolder(di, noValidation: true) : null);
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetRootAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IFolder?>(new SystemFolder(Info.Root));
    }

    /// <summary>
    /// Determines if the specified path is a file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path is a file; otherwise, <c>false</c>.</returns>
    protected static bool IsFile(string path) => global::System.IO.Path.GetFileName(path) is { } str && str != string.Empty && File.Exists(path);

    /// <summary>
    /// Determines if the specified path is a folder.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path is a folder; otherwise, <c>false</c>.</returns>
    protected static bool IsFolder(string path) => Directory.Exists(path);

    string GetParentPath(string relativePath)
    {
        // Path.GetDirectoryName() treats strings that end with a directory separator as a directory. If there's no trailing slash, it's treated as a file.
        // Run it twice for folders. The first time only shaves off the trailing directory separator.
        var parentDirectoryName = relativePath.EndsWith(global::System.IO.Path.DirectorySeparatorChar.ToString()) ? global::System.IO.Path.GetDirectoryName(global::System.IO.Path.GetDirectoryName(relativePath)) : global::System.IO.Path.GetDirectoryName(relativePath);

        // It also doesn't return a string that has a path separator at the end.
        return parentDirectoryName + global::System.IO.Path.DirectorySeparatorChar;
    }

    string GetParentDirectoryName(string relativePath)
    {
        var parentPath = GetParentPath(relativePath);
        var parentParentPath = GetParentPath(parentPath);

        return parentPath.Replace(parentParentPath, "").TrimEnd(global::System.IO.Path.DirectorySeparatorChar);
    }
}
