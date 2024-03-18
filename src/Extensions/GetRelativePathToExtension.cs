using System;
using System.Collections.Generic;
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
}