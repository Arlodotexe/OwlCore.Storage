using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A simple implementation of <see cref="IMutableStorageProperty{T}"/> that retrieves values via a delegate and supports change notifications via a watcher.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class SimpleMutableStorageProperty<T> : SimpleStorageProperty<T>, IMutableStorageProperty<T>
{
    /// <summary>
    /// Creates a new instance with a synchronous getter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="getter">A delegate that retrieves the current property value.</param>
    public SimpleMutableStorageProperty(string id, string name, Func<T> getter)
        : base(id, name, getter) { }

    /// <summary>
    /// Creates a new instance with an asynchronous getter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="asyncGetter">A delegate that asynchronously retrieves the current property value.</param>
    public SimpleMutableStorageProperty(string id, string name, Func<CancellationToken, Task<T>> asyncGetter)
        : base(id, name, asyncGetter) { }

    /// <inheritdoc/>
    public virtual Task<IStoragePropertyWatcher<T>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IStoragePropertyWatcher<T>>(new SimpleStoragePropertyWatcher<T>(this));
    }
}