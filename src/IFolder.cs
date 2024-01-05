using System.Collections.Generic;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// The simplest possible representation of a folder.
/// </summary>
public interface IFolder : IStorable
{
    /// <summary>
    /// Retrieves the folders in this directory.
    /// </summary>
    /// <param name="type">The type of items to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>An async enumerable that yields the requested items.</returns>
    IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default);
}