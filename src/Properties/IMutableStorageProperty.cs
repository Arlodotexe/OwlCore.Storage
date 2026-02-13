using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A storage property that supports watching for value changes via a disposable watcher.
/// </summary>
/// <remarks>
/// <para>
/// This interface separates change notification from value retrieval by providing a dedicated
/// <see cref="GetWatcherAsync"/> method that returns a disposable <see cref="IStoragePropertyWatcher{T}"/>.
/// </para>
/// <para>
/// This pattern parallels <see cref="IMutableFolder"/> and <see cref="IFolderWatcher"/>: the watcher
/// owns any underlying resources (e.g., <c>FileSystemWatcher</c>) and its disposal releases them,
/// keeping the property object itself lightweight and non-disposable.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IMutableStorageProperty<T> : IStorageProperty<T>
{
    /// <summary>
    /// Asynchronously retrieves a disposable watcher that can notify of changes to the property value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A disposable watcher that raises <see cref="IStoragePropertyWatcher{T}.ValueUpdated"/> when the property changes.</returns>
    Task<IStoragePropertyWatcher<T>> GetWatcherAsync(CancellationToken cancellationToken);
}
