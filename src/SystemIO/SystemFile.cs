using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.SystemIO;

/// <summary>
/// An <see cref="IFolder"/> implementation that uses System.IO.
/// </summary>
public class SystemFile : IChildFile, IFastGetRoot
{
    private string? _name;
    private FileInfo? _info;

    /// <summary>
    /// Creates a new instance of <see cref="SystemFile"/>.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    public SystemFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found at path {path}");

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
            throw new FileNotFoundException($"File not found at path {info.FullName}");

        _info = info;

        _name = _info.Name;
        Id = _info.FullName;
        Path = _info.FullName;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name => _name ??= System.IO.Path.GetFileName(Path);

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
        DirectoryInfo parent = _info != null ? _info.Directory : Directory.GetParent(Path);
        return Task.FromResult<IFolder?>(parent is { } di ? new SystemFolder(di) : null);
    }

    /// <inheritdoc />
    public Task<IFolder?> GetRootAsync()
    {
        DirectoryInfo root = _info != null ? _info.Directory.Root : new DirectoryInfo(Path).Root;
        return Task.FromResult<IFolder?>(new SystemFolder(root));
    }
}
