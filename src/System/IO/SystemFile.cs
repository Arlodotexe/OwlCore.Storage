using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// An <see cref="IFolder"/> implementation that uses System.IO.
/// </summary>
public class SystemFile : IChildFile, IGetRoot
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

        Id = path;
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
        Id = _info.FullName;
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
    /// <param name="isInternal">
    /// Dummy parameter to differiate from the public constructors.
    /// </param>
    internal SystemFile(string path, bool isInternal)
    {
        foreach (var c in global::System.IO.Path.GetInvalidPathChars())
        {
            if (path.Contains(c))
                throw new FormatException($"Provided path contains invalid character '{c}'.");
        }

        Id = path;
        Path = path;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name => _name ??= global::System.IO.Path.GetFileName(Path);

    /// <summary>
    /// Gets the path of the file on disk.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the underlying <see cref="FileInfo"/> for this folder.
    /// </summary>
    public FileInfo Info => _info ??= new(Path);

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var stream = File.Open(Path, FileMode.Open, accessMode);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<Stream>(stream);
    }

    /// <inheritdoc />
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        DirectoryInfo? parent = _info != null ? _info.Directory : Directory.GetParent(Path);
        return Task.FromResult<IFolder?>(parent != null ? new SystemFolder(parent, isInternal: true) : null);
    }

    /// <inheritdoc />
    public Task<IFolder?> GetRootAsync(CancellationToken cancellationToken = default)
    {
        DirectoryInfo root = _info?.Directory != null ? _info.Directory.Root : new DirectoryInfo(Path).Root;
        return Task.FromResult<IFolder?>(new SystemFolder(root, isInternal: true));
    }
}
