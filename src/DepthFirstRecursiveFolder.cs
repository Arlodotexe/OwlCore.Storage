using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// A wrapper around an <see cref="IFolder"/> that provides depth-first recursive iteration through all child items.
/// This class allows traversal of an entire folder hierarchy using depth-first search algorithm.
/// </summary>
public class DepthFirstRecursiveFolder : IFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DepthFirstRecursiveFolder"/> class.
    /// </summary>
    /// <param name="rootFolder">The root folder to recursively iterate through.</param>
    public DepthFirstRecursiveFolder(IFolder rootFolder)
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
    /// When null, there is no depth limit. When set to a value, recursion will stop at that depth level.
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// Asynchronously enumerates through all items in the folder hierarchy using depth-first search.
    /// This method recursively traverses through all subfolders and returns items that match the specified type filter.
    /// </summary>
    /// <param name="type">The type of items to return. Use <see cref="StorableType.All"/> to return all items,
    /// <see cref="StorableType.File"/> to return only files, or <see cref="StorableType.Folder"/> to return only folders.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the enumeration operation.</param>
    /// <returns>An async enumerable of <see cref="IStorableChild"/> items found during the depth-first traversal.</returns>
    /// <remarks>
    /// The traversal uses a stack-based approach to implement depth-first search without recursion.
    /// Items are yielded in the order they are discovered during the depth-first traversal.
    /// If <see cref="MaxDepth"/> is set, the traversal will not exceed that depth level.
    /// </remarks>
    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // DFS folder iteration using a stack of enumerators
        var ongoing = new Stack<IAsyncEnumerator<IStorableChild>>();

        try
        {
            // Start with the root folder
            ongoing.Push(RootFolder.GetItemsAsync(StorableType.All, cancellationToken).GetAsyncEnumerator(cancellationToken));

            while (ongoing.Count > 0)
            {
                var currentEnumerator = ongoing.Peek();

                // Try to get the next item from current enumerator
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

                    // If it's a folder and we haven't reached max depth, add it to the stack
                    if (item is IFolder folder)
                    {
                        if (MaxDepth is null || ongoing.Count < MaxDepth)
                            ongoing.Push(folder.GetItemsAsync(StorableType.All, cancellationToken).GetAsyncEnumerator(cancellationToken));
                    }
                }
                else
                {
                    // Current enumerator is exhausted, dispose and remove it
                    await currentEnumerator.DisposeAsync();
                    ongoing.Pop();
                }
            }
        }
        finally
        {
            // Dispose all remaining enumerators
            while (ongoing.Count > 0)
            {
                await ongoing.Pop().DisposeAsync();
            }
        }
    }
}
