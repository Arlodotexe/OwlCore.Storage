using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Represents a storage property that can be identified and whose value can be retrieved asynchronously.
/// </summary>
/// <remarks>
/// <para>
/// Storage properties extend <see cref="IStorable"/> and follow a similar lifecycle pattern to files:
/// identify → open/read → (optionally watch/write) → dispose. The property object itself is a storable
/// with its own <see cref="IStorable.Id"/> and <see cref="IStorable.Name"/>.
/// </para>
/// <para>
/// The recommended pattern is to expose a property getter (e.g., <c>ICreatedAtProperty CreatedAt { get; }</c>)
/// on an interface implemented by the storage item, then call <see cref="GetValueAsync"/> on the returned property object.
/// </para>
/// <para>
/// For mutability (change notifications), use <see cref="IMutableStorageProperty{T}"/>.
/// For modifiability (updating values), use <see cref="IModifiableStorageProperty{T}"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IStorageProperty<T> : IStorable
{
    /// <summary>
    /// Asynchronously retrieves the current property value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the current property value.</returns>
    Task<T> GetValueAsync(CancellationToken cancellationToken);
}
