using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// A file implementation which holds a reference to the provided <see cref="Stream"/> and returns it either wrapped in a non-disposable wrapper or directly, based on the <see cref="ShouldDispose"/> property.
/// </summary>
public class StreamFile : IFile
{
    /// <summary>
    /// Gets the stream being accessed for this file.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Gets a value indicating whether the underlying stream should be disposed when the returned stream from <see cref="OpenStreamAsync"/> is disposed.
    /// When true, the underlying stream is returned directly. When false, the stream is wrapped in a non-disposable wrapper.
    /// </summary>
    public bool ShouldDispose { get; }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="stream">An existing stream which is provided as the file contents.</param>
    public StreamFile(Stream stream)
        : this(stream, $"{stream.GetHashCode()}", $"{stream.GetHashCode()}", false)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="stream">An existing stream which is provided as the file contents.</param>
    /// <param name="shouldDispose">When true, the underlying stream will be disposed when the returned stream from <see cref="OpenStreamAsync"/> is disposed. When false, the stream is wrapped in a non-disposable wrapper.</param>
    public StreamFile(Stream stream, bool shouldDispose)
        : this(stream, $"{stream.GetHashCode()}", $"{stream.GetHashCode()}", shouldDispose)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="stream">An existing stream which is provided as the file contents.</param>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    public StreamFile(Stream stream, string id, string name)
        : this(stream, id, name, false)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="stream">An existing stream which is provided as the file contents.</param>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    /// <param name="shouldDispose">When true, the underlying stream will be disposed when the returned stream from <see cref="OpenStreamAsync"/> is disposed. When false, the stream is wrapped in a non-disposable wrapper.</param>
    public StreamFile(Stream stream, string id, string name, bool shouldDispose)
    {
        Stream = stream;
        Id = id;
        Name = name;
        ShouldDispose = shouldDispose;
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

        if (ShouldDispose)
            return Task.FromResult(Stream);

        return Task.FromResult<Stream>(new NonDisposableStreamWrapper(Stream));
    }
}