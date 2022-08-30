using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.SystemIO;

/// <summary>
/// An <see cref="IFolder"/> implementation that uses System.IO.
/// </summary>
public class SystemFile : IAddressableFile
{
    /// <summary>
    /// Creates a new instance of <see cref="SystemFolder"/>.
    /// </summary>
    /// <param name="path">The path to the folder.</param>
    public SystemFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found at path {path}");

        Id = path;
        Name = System.IO.Path.GetFileName(path);
        Path = path;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var stream = File.Open(Path, FileMode.Open, accessMode);

        return Task.FromResult<Stream>(stream);
    }

    /// <inheritdoc />
    public Task<IAddressableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IAddressableFolder?>(Directory.GetParent(Path) is { } di ? new SystemFolder(di) : null);
    }
}
