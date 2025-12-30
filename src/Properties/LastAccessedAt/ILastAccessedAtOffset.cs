using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item supports retrieving the last accessed timestamp as a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when the implementation can provide timezone offset information and the consumer needs to preserve the original local time context.
/// <see cref="DateTimeOffset"/> uniquely and unambiguously identifies a single point in time across systems.
/// </para>
/// <para>
/// Not all implementations support offset data—<see cref="DateTime"/> via <see cref="ILastAccessedAt"/> is the least common denominator.
/// It is easier to discard offset data than to add it when it doesn't exist, so this interface is optional.
/// </para>
/// <para>
/// A <c>null</c> return value indicates the timestamp is unavailable, unset, or could not be retrieved.
/// Sentinel values from underlying storage are translated to <c>null</c>.
/// </para>
/// </remarks>
public interface ILastAccessedAtOffset : ILastAccessedAt
{
    /// <summary>
    /// Asynchronously retrieves the last accessed timestamp of the storage item with timezone offset information.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the storage property with the last accessed timestamp and offset, or <c>null</c> if unavailable or unset.</returns>
    Task<IStorageProperty<DateTimeOffset?>> GetLastAccessedAtOffsetAsync(CancellationToken cancellationToken);
}
