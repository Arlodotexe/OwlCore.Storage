using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

public static partial class FolderExtensions
{
    /// <summary>
    /// Crawls the provided <paramref name="folder"/> and all subfolders for an item with the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="folder">The folder to crawl.</param>
    /// <param name="id">The <see cref="IStorable.Id"/> of the item to crawl.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing operation.</param>
    /// <exception cref="FileNotFoundException">No item with the provided <paramref name="id"/> was found in the provided <paramref name="folder"/>.</exception>
    /// <returns>The item</returns>
    public static async Task<IStorable> GetItemRecursiveAsync(this IFolder folder, string id, CancellationToken cancellationToken = default)
    {
        if (folder is IFastGetItemRecursive fastPath)
        {
            var item = await fastPath.GetItemRecursiveAsync(id, cancellationToken);
            if (item.Id != id)
            {
                throw new ArgumentException(@$"The item returned by the interface ""{nameof(IFastGetItemRecursive)}"" implemented in ""{folder.GetType()}"" does not have the expected Id ""{id}"". Actual value: ""{item.Id}"".", nameof(item));
            }

            return item;
        }

        try
        {
            // Check the item is in this directory (supports all available fast paths)
            return await folder.GetItemAsync(id, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            // Crawl the folder tree as a fallback.
            await foreach (var subFolder in folder.GetFoldersAsync(cancellationToken))
            {
                try
                {
                    // Check the subfolder recursively (supports all available fast paths)
                    return await subFolder.GetItemRecursiveAsync(id, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    // ignored, continue loop.
                }
            }
        }

        throw new FileNotFoundException($"No storage item with the ID \"{id}\" could be found.");
    }
}