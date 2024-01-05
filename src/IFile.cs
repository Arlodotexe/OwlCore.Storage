using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// The simplest possible representation of a file.
/// </summary>
public interface IFile : IStorable
{
    /// <summary>
    /// Opens a new stream to the resource.
    /// </summary>
    /// <param name="accessMode">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A stream that provides access to this file, with the specified <paramref name="accessMode"/>.</returns>
    Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default);
}
