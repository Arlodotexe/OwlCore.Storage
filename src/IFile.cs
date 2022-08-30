using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// The minimal functional requirements for a file.
/// </summary>
public interface IFile : IStorable
{
    /// <summary>
    /// Opens a new stream to the file.
    /// </summary>
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default);
}
