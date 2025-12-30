using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// An <see cref="IFile"/> implementation that uses System.IO.
/// </summary>
public class SystemFile : IChildFile, IGetRoot, IModifiableCreatedAtOffset, IModifiableLastAccessedAt, IModifiableLastModifiedAt
{
    private string? _name;
    private FileInfo? _info;

    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    public SystemFile(string path)
    {
        foreach (var c in global::System.IO.Path.GetInvalidPathChars())
        {
            if (path.Contains(c))
                throw new FormatException($"Provided path contains invalid character '{c}'.");
        }

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found at path {path}.");

        Path = path;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>.
    /// </summary>
    /// <param name="info">The file info.</param>
    public SystemFile(FileInfo info)
    {
        if (!info.Exists)
            throw new FileNotFoundException($"File not found at path '{info.FullName}'.");

        _info = info;

        _name = _info.Name;
        Path = _info.FullName;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>
    /// </summary>
    /// <remarks>
    /// NOTE: This constructor does not verify whether the file
    /// actually exists beforehand. Do not use outside of enumeration
    /// or when it's known that the file exists.
    /// </remarks>
    /// <param name="path">The path to the file.</param>
    /// <param name="noValidation">
    /// A required value for this overload. No functional difference between provided values.
    /// </param>
    internal SystemFile(string path, bool noValidation)
    {
        Path = path;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>
    /// </summary>
    /// <remarks>
    /// NOTE: This constructor does not verify whether the file
    /// actually exists beforehand. Do not use outside of enumeration
    /// or when it's known that the file exists.
    /// </remarks>
    /// <param name="info">The file info.</param>
    /// <param name="noValidation">
    /// A required value for this overload. No functional difference between provided values.
    /// </param>
    internal SystemFile(FileInfo info, bool noValidation)
    {
        _info = info;

        _name = _info.Name;
        Path = _info.FullName;
    }

    /// <inheritdoc />
    public string Id => Path;

    /// <inheritdoc />
    public string Name => _name ??= global::System.IO.Path.GetFileName(Path);

    /// <summary>
    /// Gets the path of the file on disk.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Contains constants for controlling the kind of access other operations can have to the same file.
    /// </summary>
    /// <remarks>
    /// This enumeration supports a bitwise combination of its member values. A typical use of this enumeration is to define whether two processes can simultaneously read from the same file. For example, if a file is opened and Read is specified, other users can open the file for reading but not for writing.
    /// </remarks>
    public FileShare FileShare { get; set; } = FileShare.None;

    /// <summary>
    /// A positive Int32 value greater than 0 indicating the buffer size. The default buffer size is 4096.
    /// </summary>
    public int BufferSize { get; set; } = 4096;

    /// <summary>
    /// Gets the underlying <see cref="FileInfo"/> for this folder.
    /// </summary>
    public FileInfo Info => _info ??= new(Path);

    /// <inheritdoc />
    public virtual Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var stream = new FileStream(Path, FileMode.Open, accessMode, FileShare, BufferSize, FileOptions.Asynchronous);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<Stream>(stream);
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        DirectoryInfo? parent = _info != null ? _info.Directory : Directory.GetParent(Path);
        return Task.FromResult<IFolder?>(parent != null ? new SystemFolder(parent, noValidation: true) : null);
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetRootAsync(CancellationToken cancellationToken = default)
    {
        DirectoryInfo root = _info?.Directory != null ? _info.Directory.Root : new DirectoryInfo(Path).Root;
        return Task.FromResult<IFolder?>(new SystemFolder(root, noValidation: true));
    }

    /// <inheritdoc />
    public Task<IStorageProperty<DateTime?>> GetCreatedAtAsync(CancellationToken cancellationToken)
        => Task.FromResult<IStorageProperty<DateTime?>>(new SimpleStorageProperty<DateTime?>(Info.CreationTime));

    /// <inheritdoc />
    public Task UpdateCreatedAtAsync(DateTime createdDateTime, CancellationToken cancellationToken)
    {
        Info.CreationTime = createdDateTime;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IStorageProperty<DateTimeOffset?>> GetCreatedAtOffsetAsync(CancellationToken cancellationToken)
        => Task.FromResult<IStorageProperty<DateTimeOffset?>>(new SimpleStorageProperty<DateTimeOffset?>(new DateTimeOffset(Info.CreationTimeUtc, TimeSpan.Zero)));

    /// <inheritdoc />
    public Task UpdateCreatedAtOffsetAsync(DateTimeOffset createdDateTime, CancellationToken cancellationToken)
    {
        Info.CreationTimeUtc = createdDateTime.UtcDateTime;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IStorageProperty<DateTime?>> GetLastAccessedAtAsync(CancellationToken cancellationToken)
        => Task.FromResult<IStorageProperty<DateTime?>>(new SimpleStorageProperty<DateTime?>(Info.LastAccessTime));

    /// <inheritdoc />
    public Task UpdateLastAccessedAtAsync(DateTime lastAccessedDateTime, CancellationToken cancellationToken)
    {
        Info.LastAccessTime = lastAccessedDateTime;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IStorageProperty<DateTime?>> GetLastModifiedAtAsync(CancellationToken cancellationToken)
        => Task.FromResult<IStorageProperty<DateTime?>>(new SimpleStorageProperty<DateTime?>(Info.LastWriteTime));

    /// <inheritdoc />
    public Task UpdateLastModifiedAtAsync(DateTime lastModifiedDateTime, CancellationToken cancellationToken)
    {
        Info.LastWriteTime = lastModifiedDateTime;
        return Task.CompletedTask;
    }
}
