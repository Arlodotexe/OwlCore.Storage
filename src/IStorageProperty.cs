using System;

namespace OwlCore.Storage;

/// <summary>
/// A property that was retrieved asynchronously, and can be disposed. The <paramref see="Value"/> can be a primitive or an object containing multiple values.
/// </summary>
/// <remarks>
/// The recommended pattern for properties is create an async method that returns <c>IStorageProperty{SomePropertyType}</c>, put that method on an interface, and use the interface to indicate support for this property value.
/// </remarks>
/// <typeparam name="T">The type used to store information about properties.</typeparam>
public interface IStorageProperty<T> : IDisposable
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