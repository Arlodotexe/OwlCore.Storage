using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

public static partial class FolderExtensions
{
    /// <summary>
    /// Crawls the ancestors of <paramref name="to" /> until <paramref name="from"/> is found, then returns the constructed relative path.
    /// </summary>
    public static async Task<string> GetRelativePathToAsync(this IFolder from, IStorableChild to, CancellationToken cancellationToken = default)
    {
        if (Equals(from, to) || from.Id == to.Id)
            return @"/";

        var pathComponents = new List<string>
        {
            to.Name,
        };
        
        cancellationToken.ThrowIfCancellationRequested();
        await RecursiveAddParentToPathAsync(to);

        // Relative path to a folder should end with a directory separator '/'
        // Relative path to a file should end with the file name.
        return to switch
        {
            IFolder => $"/{string.Join(@"/", pathComponents)}/",
            IFile => $"/{string.Join(@"/", pathComponents)}",
            _ => throw new NotSupportedException($"{to.GetType()} is not an implementation of {nameof(IFile)} or {nameof(IFolder)}. Unable to generate a path."),
        };

        async Task RecursiveAddParentToPathAsync(IStorableChild item)
        {
            var parent = await item.GetParentAsync(cancellationToken);
            if (parent is IStorableChild child && parent.Id != from.Id)
            {
                pathComponents.Insert(0, parent.Name);
                await RecursiveAddParentToPathAsync(child);
            }
        }
    }

    /// <summary>
    /// Crawls the ancestors of <paramref name="to"/> until <paramref name="from"/> is found, yielding each item along the path
    /// in traversal order from the child directly under <paramref name="from"/> down to <paramref name="to"/>.
    /// </summary>
    /// <remarks>
    /// If <paramref name="to"/> is the same item as <paramref name="from"/>, the sequence is empty.
    /// </remarks>
    /// <param name="from">The folder to which the relative path is calculated.</param>
    /// <param name="to">The target descendant item.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async sequence of items representing the path components, excluding <paramref name="from"/>.</returns>
    public static async IAsyncEnumerable<IStorable> GetItemsAlongRelativePathToAsync(this IFolder from, IStorableChild to, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // If the starting folder is the same as the target, nothing to traverse.
        if (Equals(from, to) || from.Id == to.Id)
            yield break;

        // Build the chain upward: [to, parent, parent-of-parent, ...] until reaching 'from' (exclusive)
        var chain = new List<IStorable>();

        cancellationToken.ThrowIfCancellationRequested();

        var current = to;
        while (current is not null)
        {
            chain.Add(current);

            var parent = await current.GetParentAsync(cancellationToken);
            if (parent is null)
                break;

            if (parent.Id == from.Id)
                break; // Reached the anchor; do not include 'from'

            if (parent is IStorableChild parentChild)
            {
                current = parentChild;
            }
            else
            {
                // Parent isn't a child (e.g., a root). Stop to mirror original path behavior.
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        // Yield from the item directly under 'from' down to 'to' to match the order of the string path.
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            yield return chain[i];
        }
    }
}