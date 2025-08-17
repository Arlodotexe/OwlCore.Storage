
using System;
using System.IO;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A stream wrapper that limits the number of bytes that can be read or written.
/// </summary>
/// <param name="Stream">The underlying stream to wrap around.</param>
/// <param name="MaxLength">The maximum length for this stream</param>
public class TruncatedStream(Stream Stream, long MaxLength) : Stream, IAsyncDisposable
{
    private long _position;

    /// <summary>
    /// The max length to read from the underlying <see cref="Stream"/>.
    /// </summary>
    public long MaxLength { get; set; } = MaxLength;

    /// <summary>
    /// The underlying stream to read from.
    /// </summary>
    public Stream Stream { get; } = Stream;

    /// <inheritdoc/>
    public override bool CanRead => Stream.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => Stream.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => Math.Min(Stream.Length, MaxLength);

    /// <inheritdoc/>
    public override long Position { get => Stream.Position; set => Stream.Position = value; }

    /// <inheritdoc/>
    public override void Flush() => Stream.Flush();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position + count > MaxLength)
            count = (int)(MaxLength - _position);

        var bytesRead = Stream.Read(buffer, offset, count);
        _position += bytesRead;
        return bytesRead;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (offset > MaxLength && origin == SeekOrigin.Begin)
            throw new ArgumentOutOfRangeException($"Given length value exceeds {nameof(MaxLength)} {MaxLength}");

        return Stream.Seek(offset, origin);
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        if (value > MaxLength)
            throw new ArgumentOutOfRangeException($"Given length value exceeds {nameof(MaxLength)} {MaxLength}");

        Stream.SetLength(value);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Size limiting not available for writes.");
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose the source stream synchronously
            Stream.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Dispose the source stream asynchronously if it supports it
        if (Stream is IAsyncDisposable asyncDisposableStream)
            await asyncDisposableStream.DisposeAsync();

        // Dispose of unmanaged resources.
        Dispose(false);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
}
