using System;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>
/// A property watcher that raises events when a specific property changes.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class MemoryPropertyWatcher<T> : IStoragePropertyWatcher<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryPropertyWatcher{T}"/>.
    /// </summary>
    /// <param name="property">The property being watched.</param>
    /// <param name="valueUpdated">An instance that can notify of a value update.</param>
    internal MemoryPropertyWatcher(IStorageProperty<T> property, IUpdateValue<T> valueUpdated)
    {
        Property = property;
        Inner = valueUpdated;

        valueUpdated.Updated += OnChanged;
    }

    private void OnChanged(object? sender, T e) => ValueUpdated?.Invoke(this, e);

    /// <inheritdoc/>
    public IStorageProperty<T> Property { get; }

    internal IUpdateValue<T> Inner { get; }

    /// <inheritdoc/>
    public event EventHandler<T>? ValueUpdated;

    /// <inheritdoc/>
    public void Dispose()
    {
        Inner.Updated -= OnChanged;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
