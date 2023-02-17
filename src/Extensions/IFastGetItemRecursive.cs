using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="FolderExtensions.GetItemRecursiveAsync"/> extension method.
/// </summary>
public interface IFastGetItemRecursive : IFolder
{
    /// <summary>
    /// Crawls this folder and all subfolders for an item with the provided <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The <see cref="IStorable.Id"/> of the item to crawl.</param>
    /// <param name="cancellationToken">A token to cancel the ongoing operation.</param>
    /// <exception cref="FileNotFoundException">A named item was specified in a folder, but the item wasn't found.</exception>
    /// <returns></returns>
    public Task<IStorable> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default);
}