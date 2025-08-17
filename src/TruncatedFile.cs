using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A file wrapper that limits the underlying stream to a given length.
/// </summary>
/// <param name="File">The file to wrap.</param>
/// <param name="MaxLength">The maximum byte length for this file.</param>
public class TruncatedFile(IFile File, long MaxLength) : IFile
{
    /// <summary>
    /// The file to wrap.
    /// </summary>
    public IFile File { get; } = File;

    /// <summary>
    /// The maximum byte length for this file.
    /// </summary>
    public long MaxLength { get; set; } = MaxLength;

    /// <inheritdoc/>
    public string Id => File.Id;

    /// <inheritdoc/>
    public string Name => File.Name;

    /// <inheritdoc/>
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
    {
        var fileStream = await File.OpenStreamAsync(accessMode, cancellationToken);
        return new TruncatedStream(fileStream, MaxLength);
    }
}
