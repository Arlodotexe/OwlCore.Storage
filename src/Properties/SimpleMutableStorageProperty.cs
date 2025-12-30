using System;

namespace OwlCore.Storage;

/// <summary>
/// A mutable implementation of <see cref="IStorageProperty{T}"/> that stores a property value and supports change notifications.
/// </summary>
/// <typeparam name="T">The type used to store information about properties.</typeparam>
/// <param name="PropValue">The current property value.</param>
public class SimpleMutableStorageProperty<T>(T PropValue) : SimpleStorageProperty<T>(PropValue), IMutableStorageProperty<T>
{
    /// <inheritdoc/>
    public event EventHandler<T>? ValueUpdated;

    /// <inheritdoc/>
    public void RaiseValueUpdated(T updatedValue) => ValueUpdated?.Invoke(this, updatedValue);
}