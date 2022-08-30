using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Represents a storable resource that resides within a traversable folder structure.
/// </summary>
public interface IAddressableStorable : IStorable
{
    /// <summary>
    /// A well formed string that indicates the location of the resource within a traversable folder structure. Should be relative to the topmost folder that's available when recursively crawling the return value of <see cref="GetParentAsync"/>.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the containing folder for this item, if any.
    /// </summary>
    public Task<IAddressableFolder?> GetParentAsync(CancellationToken cancellationToken = default); 
}