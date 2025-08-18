
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
    // For non-seekable streams, we track how many bytes have been consumed to enforce the window.
    private long _consumed;

    // For seekable streams, capture the starting offset to define the truncation window.
    private readonly long _startOffset = Stream.CanSeek ? Stream.Position : 0;

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
    public override long Length
    {
        get
        {
            if (Stream.CanSeek)
            {
                var available = Math.Max(0, Stream.Length - _startOffset);
                return Math.Min(available, MaxLength);
            }

            // For non-seekable, report the maximum window; actual reads will stop at EOF earlier if needed.
            return MaxLength;
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get
        {
            if (Stream.CanSeek)
                return Math.Max(0, Stream.Position - _startOffset);

            // Delegate to underlying stream; may throw if not supported
            return Stream.Position;
        }
        set
        {
            if (!Stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");

            if (value < 0 || value > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(value), $"Position must be within [0, {MaxLength}].");

            Stream.Position = _startOffset + value;
        }
    }

    /// <inheritdoc/>
    public override void Flush() => Stream.Flush();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (count <= 0)
            return 0;

        long consumed;
        if (Stream.CanSeek)
        {
            consumed = Math.Max(0, Stream.Position - _startOffset);
        }
        else
        {
            consumed = _consumed;
        }

        if (consumed >= MaxLength)
            return 0;

        var remaining = MaxLength - consumed;
        if (remaining <= 0)
            return 0;

        var allowed = (int)Math.Min(count, remaining);
        var bytesRead = Stream.Read(buffer, offset, allowed);

        if (!Stream.CanSeek)
            _consumed += bytesRead;

        return bytesRead;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (!Stream.CanSeek)
            throw new NotSupportedException("Stream does not support seeking.");

        long targetLocal = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => MaxLength + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (targetLocal < 0 || targetLocal > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(offset), $"Seek target must be within [0, {MaxLength}].");

        var underlyingPos = _startOffset + targetLocal;
        var result = Stream.Seek(underlyingPos, SeekOrigin.Begin);

        // Return the local position
        return result - _startOffset;
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        if (!Stream.CanSeek)
            throw new NotSupportedException("Stream does not support seeking.");

        if (value > MaxLength)
            throw new ArgumentOutOfRangeException($"Given length value exceeds {nameof(MaxLength)} {MaxLength}");

        // Adjust underlying length relative to the window start
        Stream.SetLength(_startOffset + value);
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
