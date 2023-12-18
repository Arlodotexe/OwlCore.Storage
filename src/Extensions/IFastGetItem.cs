using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="FolderExtensions.GetItemAsync"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface IFastGetItem : IFolder
{
    /// <summary>
    /// Retrieves the <see cref="IStorable"/> item which has the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="IStorable.Id"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An instance of <see cref="IStorableChild"/> with the requested <paramref name="id"/>.</returns>
    Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default);
}
