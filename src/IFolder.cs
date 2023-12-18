using System.Collections.Generic;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// The minimal functional requirements for a folder.
/// </summary>
public interface IFolder : IStorable
{
    /// <summary>
    /// Retrieves the folders in this directory.
    /// </summary>
    IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default);
}