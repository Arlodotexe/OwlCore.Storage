using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OwlCore.Storage;

/// <summary>
/// A folder implementation that aggregates items from multiple underlying folders.
/// This allows consumers to treat multiple distinct sources as a single virtual library.
/// </summary>
public class AggregateFolder : IFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateFolder"/> class.
    /// </summary>
    /// <param name="name">The name of this virtual folder.</param>
    /// <param name="id">A unique identifier for this virtual folder.</param>
    /// <param name="foldersToAggregate">The folders to aggregate.</param>
    public AggregateFolder(string name, string id, params IFolder[] foldersToAggregate)
    {
        Name = name;
        Id = id;
        FoldersToAggregate = new List<IFolder>(foldersToAggregate);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateFolder"/> class with a generated ID.
    /// </summary>
    /// <param name="name">The name of this virtual folder.</param>
    /// <param name="foldersToAggregate">The folders to aggregate.</param>
    public AggregateFolder(string name, params IFolder[] foldersToAggregate)
        : this(name, Guid.NewGuid().ToString(), foldersToAggregate)
    {
    }

    /// <summary>
    /// Gets the unique identifier of this folder.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name of this folder.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the list of folders being aggregated.
    /// </summary>
    public List<IFolder> FoldersToAggregate { get; }

    /// <summary>
    /// Asynchronously enumerates through all items in the aggregated folders sequentially.
    /// </summary>
    /// <param name="kind">The type of items to return.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the enumeration operation.</param>
    /// <returns>An async enumerable of <see cref="IStorable"/> items found in the aggregated folders.</returns>
    public async IAsyncEnumerable<IStorable> GetItemsAsync(
        StorableKind kind = StorableKind.All,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var folder in FoldersToAggregate)
        {
            await foreach (var item in folder.GetItemsAsync(kind, cancellationToken))
            {
                yield return item;
            }
        }
    }
}
