using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>
/// A memory stream that ignores dispose calls.
/// </summary>
internal sealed class NonDisposableStreamWrapper : Stream
{
    private readonly Stream _stream;

    /// <summary>
    /// Creates a new instance of <see cref="NonDisposableStreamWrapper"/>.
    /// </summary>
    /// <param name="stream">The stream to wrap around and prevent disposing.</param>
    public NonDisposableStreamWrapper(Stream stream)
    {
        _stream = stream;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
    }

    /// <inheritdoc />
    public override void Flush() => _stream.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => _stream.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _stream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _stream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _stream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _stream.CanWrite;

    /// <inheritdoc />
    public override long Length => _stream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }
}