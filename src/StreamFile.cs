using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage;

/// <summary>
/// A file implementation which holds a reference to the provided <see cref="StreamFile.Stream"/> and returns it in a non-disposable wrapper for <see cref="OpenStreamAsync"/>.
/// </summary>
public class StreamFile : IFile
{
    /// <summary>
    /// Gets the stream being accessed for this file.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="stream">An existing stream which is provided as the file contents.</param>
    public StreamFile(Stream stream)
        : this(stream, $"{stream.GetHashCode()}", $"{stream.GetHashCode()}")
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="stream">An existing stream which is provided as the file contents.</param>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    public StreamFile(Stream stream, string id, string name)
    {
        Stream = stream;
        Id = id;
        Name = name;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (accessMode == 0)
            throw new ArgumentOutOfRangeException(nameof(accessMode), $"{nameof(FileAccess)}.{accessMode} is not valid here.");

        return Task.FromResult<Stream>(new NonDisposableStreamWrapper(Stream));
    }
}