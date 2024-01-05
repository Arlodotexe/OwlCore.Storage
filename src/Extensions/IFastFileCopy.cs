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
    /// <param name="overwrite">If there is an existing destination file, <c>true</c> will overwrite it; otherwise <c>false</c> and the existing file is opened.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The newly created (or opened if existing) file.</returns>
    Task<IChildFile> CreateCopyOfAsync(T fileToCopy, bool overwrite = default, CancellationToken cancellationToken = default);
}
