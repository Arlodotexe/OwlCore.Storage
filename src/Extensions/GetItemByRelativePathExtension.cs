using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        // Input path separators should include all possible separators that can be used in a path.
        // This includes the directory separator, alternate directory separator, path separator, and volume separator.
        // In some scenarios, such as dotnet running on WASM on Windows, a path separator may be used that is not given by dotnet via these properties.
        // Therefore, we also include '/' and '\' as valid path separators. 
        // See also https://github.com/Arlodotexe/OwlCore.Storage/issues/86
        char[] inputPathSepChars = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.PathSeparator, Path.VolumeSeparatorChar, '/', '\\'];
        var ourPathSeparator = @"/";

        // Traverse only one level at a time
        // But recursively, until the target has been reached.
        var pathParts = relativePath.Split([.. inputPathSepChars.Distinct()]).Where(x => !string.IsNullOrWhiteSpace(x) && x != ".").ToArray();

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

    /// <summary>
    /// Navigates a relative path from a starting storable without creating any items, yielding each visited node in order.
    /// Supports '.' (no-op) and '..' (navigate to parent). Throws if a segment cannot be resolved.
    /// </summary>
    /// <param name="from">The starting item for traversal.</param>
    /// <param name="relativePath">The relative path to navigate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async sequence of visited storables (parents or children), in traversal order, excluding the starting item.</returns>
    public static async IAsyncEnumerable<IStorable> GetItemsAlongRelativePathAsync(this IStorable from, string relativePath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (from is not IFolder && from is not IStorableChild)
            throw new ArgumentException($"The starting item '{from.Name}' must be a folder or a child with a parent.", nameof(from));

        var current = from;
        var normalized = (relativePath ?? string.Empty).Replace('\\', '/');
        // Split path into parts (use API available on target framework)
#if NETSTANDARD2_0
    var parts = normalized.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
#else
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
#endif

        foreach (var raw in parts)
        {
            var segment = raw.Trim();
            if (segment.Length == 0 || segment == ".")
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            if (segment == "..")
            {
                if (current is not IStorableChild child)
                    throw new ArgumentException($"A parent folder was requested, but '{current.Name}' is not the child of a directory.", nameof(relativePath));

                var parent = await child.GetParentAsync(cancellationToken)
                             ?? throw new ArgumentOutOfRangeException(nameof(relativePath), "A parent folder was requested, but the storable item did not return a parent.");

                current = parent;
                yield return parent;
                continue;
            }

            if (current is not IFolder folder)
                throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{segment}'.");

            var next = await folder.GetFirstByNameAsync(segment)
                       ?? throw new FileNotFoundException($"A named item was specified in a folder, but the item wasn't found: '{segment}'", segment);

            current = next;
            yield return next;
        }
    }
}