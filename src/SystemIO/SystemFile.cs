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

    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="path">The path to the folder.</param>
    public SystemFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found at path {path}");

        Id = path;
        Path = path;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name => _name ??= System.IO.Path.GetFileName(Path);

    /// <inheritdoc />
    public string Path { get; }

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
        return Task.FromResult<IFolder?>(Directory.GetParent(Path) is { } di ? new SystemFolder(di) : null);
    }

    /// <inheritdoc />
    public Task<IFolder?> GetRootAsync()
    {
        return Task.FromResult<IFolder?>(new SystemFolder(new DirectoryInfo(Path).Root));
    }
}
