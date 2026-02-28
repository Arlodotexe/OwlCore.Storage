using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A storage property that supports both watching for changes and updating the value.
/// </summary>
/// <remarks>
/// <para>
/// This interface completes the readonly → mutable → modifiable interface stack for properties,
/// paralleling the folder interface hierarchy (<see cref="IFolder"/> → <see cref="IMutableFolder"/> → <see cref="IModifiableFolder"/>).
/// </para>
/// <para>
/// The value passed to <see cref="UpdateValueAsync"/> is non-nullable for value types
/// because most underlying storage systems do not support setting a null or "unset" property value.
/// Property lifecycle is tied to the property itself; there is no "delete" semantic for individual properties.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IModifiableStorageProperty<T> : IMutableStorageProperty<T>
{
    /// <summary>
    /// Asynchronously updates the property to a new value.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateValueAsync(T newValue, CancellationToken cancellationToken);
}