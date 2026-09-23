using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A <see cref="Stream"/> wrapper that, on dispose, disposes both the stream and an additional <see cref="IDisposable"/>.
/// </summary>
internal class DelegatedDisposalStream : Stream, IDelegable<IDisposable>
{
    private readonly Stream _sourceStream;

    /// <summary>
    /// Creates a new instance of <see cref="DelegatedDisposalStream"/>.
    /// </summary>
    /// <param name="sourceStream"></param>
    public DelegatedDisposalStream(Stream sourceStream)
    {
        _sourceStream = sourceStream;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            Inner.Dispose();
    }

    /// <inheritdoc />
    public override void Flush() => _sourceStream.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => _sourceStream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => _sourceStream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _sourceStream.ReadAsync(buffer, offset, count, cancellationToken);

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _sourceStream.ReadAsync(buffer, cancellationToken);
#endif
    
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => _sourceStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => _sourceStream.SetLength(value);
    
    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>  _sourceStream.Write(buffer, offset, count);

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _sourceStream.WriteAsync(buffer, offset, count, cancellationToken);

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _sourceStream.WriteAsync(buffer, cancellationToken);
#endif
    
    /// <inheritdoc />
    public override bool CanRead => _sourceStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _sourceStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _sourceStream.CanWrite;

    /// <inheritdoc />
    public override long Length => _sourceStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _sourceStream.Position;
        set => _sourceStream.Position = value;
    }

    /// <inheritdoc />
    public required IDisposable Inner { get; init; }
}