using System;

namespace OwlCore.Storage;

/// <summary>
/// The recommended pattern for file properties is create an interface with an async method that returns <c>IStorageProperties{YourPropertyClass}</c>.
/// </summary>
/// <typeparam name="T">The type used to store information about properties.</typeparam>
public interface IStorageProperties<T> : IDisposable
{
    /// <summary>
    /// Gets the current property value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Raised when the <see cref="Value"/> is updated.
    /// </summary>
    public event EventHandler<T> ValueUpdated;
}