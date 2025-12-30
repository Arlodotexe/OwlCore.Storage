using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item supports retrieving the last modified timestamp as a <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DateTime"/> is the least common denominator for timestamp properties across storage implementations.
/// The returned value may be timezone-unspecified or implied local time depending on the underlying storage system.
/// When the implementation uses UTC or when only DateTimeOffset is available, it is converted to local time.
/// </para>
/// <para>
/// A <c>null</c> return value indicates the timestamp is unavailable, unset, or could not be retrieved.
/// Sentinel values from underlying storage (e.g., <see cref="DateTime.MinValue"/>, zero) are translated to <c>null</c>.
/// </para>
/// <para>
/// When timezone offset information is required and the implementation can provide it, use <see cref="ILastModifiedAtOffset"/> instead.
/// </para>
/// </remarks>
public interface ILastModifiedAt
{
    /// <summary>
    /// Asynchronously retrieves the last modified timestamp of the storage item.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the storage property with the last modified timestamp, or <c>null</c> if unavailable or unset.</returns>
    Task<IStorageProperty<DateTime?>> GetLastModifiedAtAsync(CancellationToken cancellationToken);
}
