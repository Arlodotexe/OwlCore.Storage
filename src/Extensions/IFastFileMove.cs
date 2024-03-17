using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="ModifiableFolderExtensions.MoveFromAsync"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface IFastFileMove : IModifiableFolder
{
    /// <summary>
    /// Moves a storable item out of the provided folder, and into this folder. Returns the new item that resides in this folder.
    /// </summary>
    /// <param name="fileToMove">The file being moved into this folder.</param>
    /// <param name="source">The folder that <paramref name="fileToMove"/> is being moved from.</param>
    /// <param name="overwrite">If there is an existing destination file, <c>true</c> will overwrite it; otherwise <c>false</c> and the existing file is opened.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <param name="fallback">The fallback to use if the provided <paramref name="fileToMove"/> isn't supported.</param>
    /// <returns>The newly created (or opened if existing) file.</returns>
    Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken, MoveFromDelegate fallback);
}

/// <summary>
/// A delegate that provides a fallback for the <see cref="IFastFileMove.MoveFromAsync"/> method.
/// </summary>
public delegate Task<IChildFile> MoveFromDelegate(IModifiableFolder modifiableFolder, IChildFile file, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken);