using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for <see cref="IModifiableFolder"/>.
/// </summary>
public static partial class StorableChildExtensions
{
    /// <summary>
    /// Retrieves the root of the provided <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The item which the root should be retrieved from.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>The folder that this implementation considers the "root".</returns>
    public static async Task<IFolder?> GetRootAsync(this IStorableChild item, CancellationToken cancellationToken = default)
    {
        // If the item knows how to find the root quickly.
        if (item is IFastGetRoot fastRoot)
            return await fastRoot.GetRootAsync(cancellationToken);

        // Otherwise, manually recurse to the root.
        var parent = await item.GetParentAsync(cancellationToken);
        if (parent is not IStorableChild parentAsChild)
        {
            // Item is the root already.
            return null;
        }

        // Item is not the root, try asking the parent.
        return await parentAsChild.GetRootAsync(cancellationToken);
    }
}