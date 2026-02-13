using System;

namespace OwlCore.Storage;

/// <summary>
/// A storage property representing the creation timestamp as a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when the implementation can provide timezone offset information and the consumer needs to preserve the original local time context.
/// <see cref="DateTimeOffset"/> uniquely and unambiguously identifies a single point in time across systems.
/// </para>
/// <para>
/// Not all implementations support offset data—<see cref="DateTime"/> via <see cref="ICreatedAtProperty"/> is the least common denominator.
/// It is easier to discard offset data than to add it when it doesn't exist, so this interface is optional.
/// </para>
/// <para>
/// A <c>null</c> return value from <see cref="IStorageProperty{T}.GetValueAsync"/> indicates the timestamp is unavailable, unset, or could not be retrieved.
/// Sentinel values from underlying storage are translated to <c>null</c>.
/// </para>
/// </remarks>
public interface ICreatedAtOffsetProperty : IStorageProperty<DateTimeOffset?>
{
}
