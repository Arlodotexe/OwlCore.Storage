using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Represents a storable resource that resides within a traversable folder structure.
/// </summary>
public interface IStorableChild : IStorable
{
    /// <summary>
    /// Gets the containing folder for this item, if any.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The containing parent folder, if any.</returns>
    Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default); 
}