using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="FolderExtensions.GetFirstItemByNameAsync"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface IFolderCanFastGetFirstItemByName : IFolder
{
    /// <summary>
    /// Retrieves the first <see cref="IStorable"/> item which has the provided <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The <see cref="IStorable.Name"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns></returns>
    public Task<IAddressableStorable> GetFirstItemByNameAsync(string name, CancellationToken cancellationToken = default);
}