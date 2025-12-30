using System;

namespace OwlCore.Storage;

/// <summary>
/// A property that was retrieved asynchronously and may be updated.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMutableStorageProperty<T> : IStorageProperty<T>
{
    /// <summary>
    /// Raised when the <see cref="IStorageProperty{T}.Value"/> is updated.
    /// </summary>
    event EventHandler<T>? ValueUpdated;
}
