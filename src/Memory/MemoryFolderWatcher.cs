using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>
/// Watches a <see cref="MemoryFolder"/> for changes.
/// </summary>
internal class MemoryFolderWatcher : IFolderWatcher
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryFolderWatcher"/>.
    /// </summary>
    /// <param name="folder">The folder being watched.</param>
    public MemoryFolderWatcher(IMutableFolder folder)
    {
        Folder = folder;
    }

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    internal void NotifyItemAdded(IStorable item)
    {
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<IStorable> { item }));
    }

    internal void NotifyItemRemoved(IStorable item)
    {
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<IStorable> { item }));
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc />
    public IMutableFolder Folder { get; }
}