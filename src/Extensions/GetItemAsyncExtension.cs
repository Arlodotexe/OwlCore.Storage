using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

public static partial class FolderExtensions
{
    /// <summary>
    /// Retrieves the <see cref="IStorable"/> item which has the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="folder">The folder to get items from.</param>
    /// <param name="id">The <see cref="IStorable.Id"/> of the storable item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable which yields the items in the provided folder.</returns>
    /// <exception cref="FileNotFoundException">The item was not found in the provided folder.</exception>
    public static async Task<IStorableChild> GetItemAsync(this IFolder folder, string id, CancellationToken cancellationToken = default)
    {
        if (folder is IGetItem fastPath)
            return await fastPath.GetItemAsync(id, cancellationToken);

        var targetItem = await folder.GetItemsAsync(cancellationToken: cancellationToken).FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (targetItem is null)
            throw new FileNotFoundException($"No storage item with the Id '{id}' could be found.");

        return targetItem;
    }
}