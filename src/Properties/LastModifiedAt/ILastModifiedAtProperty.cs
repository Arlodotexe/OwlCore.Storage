using System;

namespace OwlCore.Storage;

/// <summary>
/// A storage property representing the last modified timestamp as a <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DateTime"/> is the least common denominator for timestamp properties across storage implementations.
/// The returned value may be timezone-unspecified or implied local time depending on the underlying storage system.
/// When the implementation uses UTC or when only <see cref="DateTimeOffset"/> is available, it is converted to local time.
/// </para>
/// <para>
/// A <c>null</c> return value from <see cref="IStorageProperty{T}.GetValueAsync"/> indicates the timestamp is unavailable, unset, or could not be retrieved.
/// Sentinel values from underlying storage (e.g., <see cref="DateTime.MinValue"/>, zero) are translated to <c>null</c>.
/// </para>
/// <para>
/// When timezone offset information is required and the implementation can provide it, use <see cref="ILastModifiedAtOffsetProperty"/> instead.
/// </para>
/// </remarks>
public interface ILastModifiedAtProperty : IStorageProperty<DateTime?>
{
}
