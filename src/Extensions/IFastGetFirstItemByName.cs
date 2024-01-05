using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="FolderExtensions.GetFirstByNameAsync"/> extension method.
/// </summary>
/// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
public interface IFastGetFirstByName : IFolder
{
    /// <summary>
    /// Retrieves the first <see cref="IStorable"/> item which has the provided <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The <see cref="IStorable.Name"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The first <see cref="IStorable"/> with the requested <paramref name="name"/>.</returns>
    Task<IStorableChild> GetFirstByNameAsync(string name, CancellationToken cancellationToken = default);
}