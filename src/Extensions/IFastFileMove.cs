using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="ModifiableFolderExtensions.MoveFromAsync{T}"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface IFastFileMove<in T> : IModifiableFolder
    where T : IFile, IStorableChild
{
    /// <summary>
    /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
    /// </summary>
    /// <param name="fileToMove">The file being moved into this folder.</param>
    /// <param name="source">The folder that <paramref name="fileToMove"/> is being moved from.</param>
    /// <param name="overwrite">If there is an existing destination file, <c>true</c> will overwrite it; otherwise <c>false</c> and the existing file is opened.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The newly created (or opened if existing) file.</returns>
    Task<IChildFile> MoveFromAsync(T fileToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default);
}