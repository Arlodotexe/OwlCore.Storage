using System;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A simple watcher implementation for <see cref="SimpleMutableStorageProperty{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class SimpleStoragePropertyWatcher<T> : IStoragePropertyWatcher<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="SimpleStoragePropertyWatcher{T}"/>.
    /// </summary>
    /// <param name="property">The property being watched.</param>
    public SimpleStoragePropertyWatcher(IStorageProperty<T> property)
    {
        Property = property;
    }

    /// <inheritdoc/>
    public IStorageProperty<T> Property { get; }

    /// <inheritdoc/>
    public event EventHandler<T>? ValueUpdated;

    /// <summary>
    /// Raises the <see cref="ValueUpdated"/> event.
    /// </summary>
    /// <param name="updatedValue">The new value.</param>
    internal void RaiseValueUpdated(T updatedValue) => ValueUpdated?.Invoke(this, updatedValue);

    /// <inheritdoc/>
    public void Dispose() { }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;
}