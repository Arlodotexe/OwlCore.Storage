using System;
using System.IO;

namespace OwlCore.Storage;

/// <summary>
/// Wraps around a non-seekable stream to enable seeking functionality with lazy loading of the source.
/// </summary>
internal class LazySeekStream : Stream
{
    private readonly Stream _originalStream;
    private readonly MemoryStream _memoryStream;

    /// <summary>
    /// Creates a new instance of <see cref="LazySeekStream"/>.
    /// </summary>
    /// <param name="stream"></param>
    public LazySeekStream(Stream stream)
    {
        _originalStream = stream;

        _memoryStream = new MemoryStream()
        {
            Capacity = (int)Length,
        };
    }

    /// <inheritdoc />
    public override bool CanRead => _memoryStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _memoryStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _originalStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _memoryStream.Position;
        set
        {
            if (value < 0)
                throw new IOException("An attempt was made to move the position before the beginning of the stream.");

            // Check if the requested position is beyond the current length of the memory stream
            if (value > _memoryStream.Length)
            {
                long additionalBytesNeeded = value - _memoryStream.Length;
                var buffer = new byte[additionalBytesNeeded];
                long totalBytesRead = 0;

                while (totalBytesRead < additionalBytesNeeded)
                {
                    int bytesRead = _originalStream.Read(buffer, (int)totalBytesRead, (int)(additionalBytesNeeded - totalBytesRead));
                    if (bytesRead == 0)
                        break; // End of the original stream reached

                    totalBytesRead += bytesRead;
                }

                // Write the newly read bytes to the end of the memory stream
                _memoryStream.Seek(0, SeekOrigin.End);
                _memoryStream.Write(buffer, 0, (int)totalBytesRead);
            }

            // Set the new position of the memory stream
            _memoryStream.Position = value;
        }
    }

    /// <inheritdoc />
    public override void Flush() => _memoryStream.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        // Read from memory stream first
        if (_memoryStream.Position < _memoryStream.Length)
        {
            totalBytesRead = _memoryStream.Read(buffer, offset, count);
            if (totalBytesRead == count)
            {
                return totalBytesRead; // Complete read from memory stream
            }

            // Prepare to read the remaining data from the original stream
            offset += totalBytesRead;
            count -= totalBytesRead;
        }

        // Read the remaining data directly into the provided buffer
        while (count > 0)
        {
            int bytesReadFromOriginalStream = _originalStream.Read(buffer, offset, count);
            if (bytesReadFromOriginalStream == 0)
            {
                break; // End of the original stream reached
            }

            // Write the new data from the original stream into the memory stream
            _memoryStream.Seek(0, SeekOrigin.End);
            _memoryStream.Write(buffer, offset, bytesReadFromOriginalStream);

            totalBytesRead += bytesReadFromOriginalStream;
            offset += bytesReadFromOriginalStream;
            count -= bytesReadFromOriginalStream;
        }

        return totalBytesRead;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position = _memoryStream.Position + offset;
                break;
            case SeekOrigin.End:
                Position = _originalStream.Length + offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), "Invalid seek origin.");
        }

        return Position;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Length must be non-negative.");

        if (value < _memoryStream.Length)
        {
            // Truncate the memory stream
            _memoryStream.SetLength(value);
        }
        else if (value > _memoryStream.Length)
        {
            long additionalBytesNeeded = value - _memoryStream.Length;

            // Extend the memory stream with zeros or additional data from the original stream
            if (_originalStream.CanRead && additionalBytesNeeded > 0)
            {
                var buffer = new byte[additionalBytesNeeded];
                int bytesRead = _originalStream.Read(buffer, 0, buffer.Length);

                _memoryStream.Seek(0, SeekOrigin.End);
                _memoryStream.Write(buffer, 0, bytesRead);

                if (bytesRead < additionalBytesNeeded)
                {
                    // Fill the rest with zeros if the original stream didn't have enough data
                    var zeroFill = new byte[additionalBytesNeeded - bytesRead];
                    _memoryStream.Write(zeroFill, 0, zeroFill.Length);
                }
            }
            else
            {
                // Fill with zeros if the original stream can't be read or no additional bytes are needed
                _memoryStream.SetLength(value);
            }
        }
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException($"Writing not supported by {nameof(LazySeekStream)}");

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _memoryStream.Dispose();
        _originalStream.Dispose();
    }
}
