using System;
using System.IO;

namespace OwlCore.Storage;

/// <summary>
/// A stream wrapper that allows overriding the Length property.
/// </summary>
internal class LengthOverrideStream : Stream
{
    private readonly long _overriddenLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthOverrideStream"/> class.
    /// </summary>
    /// <param name="sourceStream">The underlying stream to wrap.</param>
    /// <param name="overriddenLength">The length value to be returned by the Length property.</param>
    public LengthOverrideStream(Stream sourceStream, long overriddenLength)
    {
        SourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
        _overriddenLength = overriddenLength;
    }

    /// <summary>
    /// The underlying source stream being wrapped around.
    /// </summary>
    public Stream SourceStream { get; }

    /// <inheritdoc />
    public override bool CanRead => SourceStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => SourceStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => SourceStream.CanWrite;

    /// <inheritdoc />
    public override long Length => _overriddenLength;

    /// <inheritdoc />
    public override long Position
    {
        get => SourceStream.Position;
        set => SourceStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush() => SourceStream.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => SourceStream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => SourceStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => SourceStream.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => SourceStream.Write(buffer, offset, count);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SourceStream.Dispose();

        base.Dispose(disposing);
    }
}
