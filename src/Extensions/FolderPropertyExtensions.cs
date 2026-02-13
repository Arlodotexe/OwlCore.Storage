namespace OwlCore.Storage;

/// <summary>
/// Extension methods for file properties.
/// </summary>
public static class FolderPropertyExtensions
{
    extension(IFolder storable)
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
}
