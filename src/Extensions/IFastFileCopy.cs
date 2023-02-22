using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="ModifiableFolderExtensions.CreateCopyOfAsync{T}"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface IFastFileCopy<in T> : IModifiableFolder
    where T : IFile
{
    /// <summary>
    /// Creates a copy of the provided file within this folder.
    /// </summary>
    /// <param name="fileToCopy">The file to be copied into this folder.</param>
    /// <param name="overwrite"><code>true</code> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task<IChildFile> CreateCopyOfAsync(T fileToCopy, bool overwrite = default, CancellationToken cancellationToken = default);
}
