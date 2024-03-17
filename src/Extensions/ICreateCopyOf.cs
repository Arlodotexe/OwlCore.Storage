using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a way for implementations to override behavior of the <see cref="ModifiableFolderExtensions.CreateCopyOfAsync"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface ICreateCopyOf : IModifiableFolder
{
    /// <summary>
    /// Creates a copy of the provided file within this folder.
    /// </summary>
    /// <param name="fileToCopy">The file to be copied into this folder.</param>
    /// <param name="overwrite">If there is an existing destination file, <c>true</c> will overwrite it; otherwise <c>false</c> and the existing file is opened.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <param name="fallback">The fallback to use if the provided <paramref name="fileToCopy"/> isn't supported.</param>
    /// <returns>The newly created (or opened if existing) file.</returns>
    Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite, CancellationToken cancellationToken, CreateCopyOfDelegate fallback);
}

/// <summary>
/// A delegate that provides a fallback for the <see cref="IMoveFrom.MoveFromAsync"/> method.
/// </summary>
/// <returns></returns>
public delegate Task<IChildFile> CreateCopyOfDelegate(IModifiableFolder destination, IFile fileToCopy, bool overwrite, CancellationToken cancellationToken);
