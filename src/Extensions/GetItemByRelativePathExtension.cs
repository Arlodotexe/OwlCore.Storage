using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for <see cref="IStorable"/>.
/// </summary>
public static partial class StorableExtensions
{
    /// <summary>
    /// From the provided <see cref="IStorable"/>, traverses the provided relative path and returns the item at that path.
    /// </summary>
    /// <param name="from">The item to start with when traversing.</param>
    /// <param name="relativePath">The path of the storable item to return, relative to the provided item.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing operation.</param>
    /// <returns>The <see cref="IStorable"/> item found at the relative path.</returns>
    /// <exception cref="ArgumentException">
    /// A parent directory was specified, but the provided <see cref="IStorable"/> is not addressable.
    /// Or, the provided relative path named a folder, but the item was a file.
    /// Or, an empty path part was found.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">A parent folder was requested, but the storable item did not return a parent.</exception>
    /// <exception cref="FileNotFoundException">A named item was specified in a folder, but the item wasn't found.</exception>
    public static async Task<IStorable> GetItemByRelativePathAsync(this IStorable from, string relativePath, CancellationToken cancellationToken = default)
    {
        var inputPathChars = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.PathSeparator, Path.VolumeSeparatorChar };
        var ourPathSeparator = @"/";

        // Traverse only one level at a time
        // But recursively, until the target has been reached.
        var pathParts = relativePath.Split(inputPathChars).Where(x => !string.IsNullOrWhiteSpace(x) && x != ".").ToArray();

        // Current directory was specified.
        if (pathParts.Length == 0)
            return from;

        var nextPathPart = pathParts[0];
        if (string.IsNullOrWhiteSpace(nextPathPart))
            throw new ArgumentException("Empty path part found. Cannot navigate to an item without a name.", nameof(nextPathPart));

        // Get parent directory.
        if (nextPathPart == "..")
        {
            if (from is not IStorableChild child)
                throw new ArgumentException($"A parent folder was requested, but the storable item named '{from.Name}' is not the child of a directory.", nameof(relativePath));

            var parent = await child.GetParentAsync(cancellationToken);

            // If this item was the last one needed.
            if (parent is not null && pathParts.Length == 1)
                return parent;

            if (parent is null)
                throw new ArgumentOutOfRangeException(nameof(relativePath), "A parent folder was requested, but the storable item did not return a parent.");

            var newRelativePath = string.Join(ourPathSeparator, pathParts.Skip(1));
            return await GetItemByRelativePathAsync(parent, newRelativePath);
        }

        // Get item by name.
        if (from is not IFolder folder)
            throw new ArgumentException($"An item named '{nextPathPart}' was requested from the folder named '{from.Name}', but '{from.Name}' is not a folder.");

        var item = await folder.GetFirstByNameAsync(nextPathPart, cancellationToken);
        if (item is null)
            throw new FileNotFoundException($"An item named '{nextPathPart}' was requested from the folder named '{from.Name}', but '{nextPathPart}' wasn't found in the folder.");

        return await GetItemByRelativePathAsync(item, string.Join(ourPathSeparator, pathParts.Skip(1)));
    }
}