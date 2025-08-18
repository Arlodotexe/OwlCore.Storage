using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// A wrapper around an <see cref="IFolder"/> that provides breadth-first recursive iteration through all child items.
/// This class traverses an entire folder hierarchy using a breadth-first search algorithm.
/// </summary>
public class BreadthFirstRecursiveFolder : IFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreadthFirstRecursiveFolder"/> class.
    /// </summary>
    /// <param name="rootFolder">The root folder to recursively iterate through.</param>
    public BreadthFirstRecursiveFolder(IFolder rootFolder)
    {
        RootFolder = rootFolder;
    }

    /// <summary>
    /// Gets the unique identifier of the root folder.
    /// </summary>
    public string Id => RootFolder.Id;

    /// <summary>
    /// Gets the name of the root folder.
    /// </summary>
    public string Name => RootFolder.Name;

    /// <summary>
    /// Gets the root folder that this instance will recursively iterate through.
    /// </summary>
    public IFolder RootFolder { get; }

    /// <summary>
    /// Gets or initializes the maximum depth for recursive folder traversal.
    /// When null, there is no depth limit. When set to a value, traversal will stop before exceeding that depth level.
    /// Depth is measured such that the direct children of the root are at depth 1.
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// Asynchronously enumerates through all items in the folder hierarchy using breadth-first search.
    /// This method traverses through each level of subfolders and returns items that match the specified type filter.
    /// </summary>
    /// <param name="type">The type of items to return. Use <see cref="StorableType.All"/> to return all items,
    /// <see cref="StorableType.File"/> to return only files, or <see cref="StorableType.Folder"/> to return only folders.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the enumeration operation.</param>
    /// <returns>An async enumerable of <see cref="IStorableChild"/> items found during the breadth-first traversal.</returns>
    /// <remarks>
    /// The traversal uses a queue-based approach to implement breadth-first search without recursion.
    /// Items are yielded in level order: first all direct children of the root, then children of those folders, and so on.
    /// If <see cref="MaxDepth"/> is set, the traversal will not exceed that depth level.
    /// </remarks>
    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(
        StorableType type = StorableType.All,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // BFS folder iteration using a queue of enumerators with depth tracking
        var queue = new Queue<(IAsyncEnumerator<IStorableChild> Enumerator, int Depth)>();

        try
        {
            // Depth 1 = direct children of root
            queue.Enqueue((RootFolder.GetItemsAsync(StorableType.All, cancellationToken).GetAsyncEnumerator(cancellationToken), 1));

            while (queue.Count > 0)
            {
                var (currentEnumerator, currentDepth) = queue.Peek();

                if (await currentEnumerator.MoveNextAsync())
                {
                    var item = currentEnumerator.Current;

                    // Filter by type if needed
                    if (type == StorableType.All ||
                        (type == StorableType.File && item is IFile) ||
                        (type == StorableType.Folder && item is IFolder))
                    {
                        yield return item;
                    }

                    // If it's a folder and we haven't reached max depth, enqueue its children
                    if (item is IFolder folder)
                    {
                        if (MaxDepth is null || currentDepth < MaxDepth)
                        {
                            queue.Enqueue((folder.GetItemsAsync(StorableType.All, cancellationToken).GetAsyncEnumerator(cancellationToken), currentDepth + 1));
                        }
                    }
                }
                else
                {
                    // Current enumerator is exhausted, dispose and remove it
                    await currentEnumerator.DisposeAsync();
                    queue.Dequeue();
                }
            }
        }
        finally
        {
            // Dispose all remaining enumerators
            while (queue.Count > 0)
            {
                var (enumerator, _) = queue.Dequeue();
                await enumerator.DisposeAsync();
            }
        }
    }
}
