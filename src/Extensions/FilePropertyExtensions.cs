using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for file properties.
/// </summary>
public static class FilePropertyExtensions
{
    extension(IStorable storable)
    {
        /// <summary>
        /// Gets the <see cref="ICreatedAtProperty"/> from the item, or null if not supported.
        /// </summary>
        public ICreatedAtProperty? CreatedAt => (storable as ICreatedAt)?.CreatedAt;

        /// <summary>
        /// Gets the <see cref="ICreatedAtOffsetProperty"/> from the item, or null if not supported.
        /// </summary>
        public ICreatedAtOffsetProperty? CreatedAtOffset => (storable as ICreatedAtOffset)?.CreatedAtOffset;

        /// <summary>
        /// Gets the <see cref="ILastModifiedAtProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastModifiedAtProperty? LastModifiedAt => (storable as ILastModifiedAt)?.LastModifiedAt;

        /// <summary>
        /// Gets the <see cref="ILastModifiedAtOffsetProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastModifiedAtOffsetProperty? LastModifiedAtOffset => (storable as ILastModifiedAtOffset)?.LastModifiedAtOffset;

        /// <summary>
        /// Gets the <see cref="ILastAccessedAtProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastAccessedAtProperty? LastAccessedAt => (storable as ILastAccessedAt)?.LastAccessedAt;

        /// <summary>
        /// Gets the <see cref="ILastAccessedAtOffsetProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastAccessedAtOffsetProperty? LastAccessedAtOffset => (storable as ILastAccessedAtOffset)?.LastAccessedAtOffset;
    }

    /// <summary>
    /// Captures all available timestamp values from a file before operations that might modify them.
    /// </summary>
    internal static async Task<CapturedTimestamps> CaptureAllTimestampsAsync(this IFile source, CancellationToken cancellationToken)
    {
        var timestamps = new CapturedTimestamps();

        if (source.CreatedAt is { } srcCreatedAt)
        {
            try { timestamps.CreatedAt = await srcCreatedAt.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        if (source.CreatedAtOffset is { } srcCreatedAtOffset)
        {
            try { timestamps.CreatedAtOffset = await srcCreatedAtOffset.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        if (source.LastModifiedAt is { } srcLastModifiedAt)
        {
            try { timestamps.LastModifiedAt = await srcLastModifiedAt.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        if (source.LastModifiedAtOffset is { } srcLastModifiedAtOffset)
        {
            try { timestamps.LastModifiedAtOffset = await srcLastModifiedAtOffset.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        if (source.LastAccessedAt is { } srcLastAccessedAt)
        {
            try { timestamps.LastAccessedAt = await srcLastAccessedAt.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        if (source.LastAccessedAtOffset is { } srcLastAccessedAtOffset)
        {
            try { timestamps.LastAccessedAtOffset = await srcLastAccessedAtOffset.GetValueAsync(cancellationToken); }
            catch { /* Ignore */ }
        }

        return timestamps;
    }

    /// <summary>
    /// Applies all captured timestamps to a destination file.
    /// </summary>
    internal static async Task ApplyAllTimestampsAsync(this IFile destination, CapturedTimestamps timestamps, CancellationToken cancellationToken)
    {
        // Apply CreatedAt
        if (timestamps.CreatedAt.HasValue && destination.CreatedAt is IModifiableStorageProperty<DateTime?> destCreatedAt)
        {
            try { await destCreatedAt.UpdateValueAsync(timestamps.CreatedAt.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        // Apply CreatedAtOffset
        if (timestamps.CreatedAtOffset.HasValue && destination.CreatedAtOffset is IModifiableStorageProperty<DateTimeOffset?> destCreatedAtOffset)
        {
            try { await destCreatedAtOffset.UpdateValueAsync(timestamps.CreatedAtOffset.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        // Apply LastModifiedAt
        if (timestamps.LastModifiedAt.HasValue && destination.LastModifiedAt is IModifiableStorageProperty<DateTime?> destLastModifiedAt)
        {
            try { await destLastModifiedAt.UpdateValueAsync(timestamps.LastModifiedAt.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        // Apply LastModifiedAtOffset
        if (timestamps.LastModifiedAtOffset.HasValue && destination.LastModifiedAtOffset is IModifiableStorageProperty<DateTimeOffset?> destLastModifiedAtOffset)
        {
            try { await destLastModifiedAtOffset.UpdateValueAsync(timestamps.LastModifiedAtOffset.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        // Apply LastAccessedAt
        if (timestamps.LastAccessedAt.HasValue && destination.LastAccessedAt is IModifiableStorageProperty<DateTime?> destLastAccessedAt)
        {
            try { await destLastAccessedAt.UpdateValueAsync(timestamps.LastAccessedAt.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }

        // Apply LastAccessedAtOffset
        if (timestamps.LastAccessedAtOffset.HasValue && destination.LastAccessedAtOffset is IModifiableStorageProperty<DateTimeOffset?> destLastAccessedAtOffset)
        {
            try { await destLastAccessedAtOffset.UpdateValueAsync(timestamps.LastAccessedAtOffset.Value, cancellationToken); }
            catch { /* Silently continue - timestamp preservation is best-effort */ }
        }
    }

    internal struct CapturedTimestamps
    {
        public DateTime? CreatedAt;
        public DateTimeOffset? CreatedAtOffset;
        public DateTime? LastModifiedAt;
        public DateTimeOffset? LastModifiedAtOffset;
        public DateTime? LastAccessedAt;
        public DateTimeOffset? LastAccessedAtOffset;
    }
}
