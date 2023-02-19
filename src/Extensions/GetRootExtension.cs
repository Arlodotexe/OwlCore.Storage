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
    /// <returns>The folder that this implementation considers the "root".</returns>
    public static async Task<IFolder?> GetRootAsync(this IStorableChild item)
    {
        // If the item knows how to find the root quickly.
        if (item is IFastGetRoot fastRoot)
            return await fastRoot.GetRootAsync();

        // Otherwise, manually recurse to the root.
        var parent = await item.GetParentAsync();
        if (parent is null || parent is not IStorableChild parentAsChild)
        {
            // Item is the root already.
            return null;
        }
        else
        {
            // Item is not the root, try asking the parent.
            return await parentAsChild.GetRootAsync();
        }
    }
}