using System;
using System.Collections.Generic;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// Overrides the return value for the given folder's <see cref="IFolder.GetItemsAsync(StorableType, CancellationToken)"/> async enumerable.
/// </summary>
/// <param name="Folder">The folder being overridden.</param>
/// <param name="ItemsOverrideFunc">The function used to override the items yielded by <see cref="GetItemsAsync"/>.</param>
public class ItemsOverrideFolder(IFolder Folder, Func<IAsyncEnumerable<IStorableChild>, IAsyncEnumerable<IStorableChild>> ItemsOverrideFunc) : IFolder
{
    /// <summary>
    /// The folder being overridden
    /// </summary>
    public IFolder Folder { get; } = Folder;

    /// <summary>
    /// The function used to override the items yielded by <see cref="GetItemsAsync"/>. 
    /// </summary>
    public Func<IAsyncEnumerable<IStorableChild>, IAsyncEnumerable<IStorableChild>> ItemsOverrideFunc { get; } = ItemsOverrideFunc;

    /// <inheritdoc/>
    public string Id => Folder.Id;

    /// <inheritdoc/>
    public string Name => Folder.Name;

    /// <inheritdoc/>
    public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
    {
        return ItemsOverrideFunc(Folder.GetItemsAsync(type, cancellationToken));
    }
}