using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Archive;

/// <summary>
/// A file implementation wrapping a <see cref="ZipArchiveEntry"/>.
/// </summary>
public class ZipEntryFile : IAddressableFile
{
    private readonly IFolder? _parent;
    private readonly ZipArchiveEntry _entry;

    /// <summary>
    /// Creates a new instance of <see cref="ZipEntryFile"/>.
    /// </summary>
    /// <param name="entry">The archive entry for this file.</param>
    /// <param name="parent">The parent folder.</param>
    public ZipEntryFile(ZipArchiveEntry entry, IFolder? parent)
    {
        Id = Name = entry.Name;
        Path = entry.FullName;

        _parent = parent;
        _entry = entry;
    }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_parent);
    }

    /// <inheritdoc/>
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (accessMode == 0)
            throw new ArgumentOutOfRangeException(nameof(accessMode), $"{nameof(FileAccess)}.{accessMode} is not valid here.");

        return Task.FromResult(_entry.Open());
    }
}
