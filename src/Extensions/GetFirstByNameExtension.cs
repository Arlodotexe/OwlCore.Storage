using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

public static partial class FolderExtensions
{
    /// <summary>
    /// Retrieves the first <see cref="IStorable"/> item which has the provided <paramref name="name"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="name">The <see cref="IStorable.Name"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the items in the provided folder.</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IStorableChild> GetFirstByNameAsync(this IFolder folder, string name, CancellationToken cancellationToken = default)
    {
        if (folder is IGetFirstByName fastPath)
            return await fastPath.GetFirstByNameAsync(name, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken).FirstOrDefaultAsync(x => name.Equals(x.Name, StringComparison.Ordinal), cancellationToken);
        if (targetItem is null)
        {
            throw new FileNotFoundException($"No storage item with the name \"{name}\" could be found.");
        }

        return targetItem;
    }
}