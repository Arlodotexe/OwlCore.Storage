using System;
using OwlCore.Storage;

namespace OwlCore.Storage;

/// <summary>
/// A property that was retrieved asynchronously. The <see cref="Value"/> can be a primitive or an object containing multiple values.
/// </summary>
/// <remarks>
/// <para>
/// The recommended pattern for properties is to create an async method that returns <c>IStorageProperty{SomePropertyType}</c>, 
/// put that method on an interface, and use the interface to indicate optional support for this property value.
/// </para>
/// <para>
/// For modifiability, implement a parallel <c>IModifiable*</c> interface with an <c>Update*Async</c> method.
/// Get and notify are bundled into the "get" method (with notify behind an optional interface), 
/// while get/set are kept as parallel methods on separate interfaces.
/// </para>
/// <para>
/// Property lifecycle is tied to the storage container (file or folder), not to the property itself.
/// Disposal responsibility belongs to the construction site—either delegate lifecycle to the consumer-constructed container,
/// or have the consumer check <see cref="IDisposable"/> on each leaf object returned by the disposable root instance.
/// </para>
/// </remarks>
/// <typeparam name="T">The type used to store information about properties.</typeparam>
public interface IStorageProperty<T>
{
    /// <summary>
    /// Gets the current property value.
    /// </summary>
    T Value { get; }
}
