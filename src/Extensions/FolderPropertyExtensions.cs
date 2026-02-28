namespace OwlCore.Storage;

/// <summary>
/// Extension methods for file properties.
/// </summary>
public static class FolderPropertyExtensions
{
    extension(IFolder folder)
    {
        /// <summary>
        /// Gets the <see cref="ICreatedAtProperty"/> from the item, or null if not supported.
        /// </summary>
        public ICreatedAtProperty? CreatedAt => (folder as ICreatedAt)?.CreatedAt;

        /// <summary>
        /// Gets the <see cref="ICreatedAtOffsetProperty"/> from the item, or null if not supported.
        /// </summary>
        public ICreatedAtOffsetProperty? CreatedAtOffset => (folder as ICreatedAtOffset)?.CreatedAtOffset;

        /// <summary>
        /// Gets the <see cref="ILastModifiedAtProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastModifiedAtProperty? LastModifiedAt => (folder as ILastModifiedAt)?.LastModifiedAt;

        /// <summary>
        /// Gets the <see cref="ILastModifiedAtOffsetProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastModifiedAtOffsetProperty? LastModifiedAtOffset => (folder as ILastModifiedAtOffset)?.LastModifiedAtOffset;

        /// <summary>
        /// Gets the <see cref="ILastAccessedAtProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastAccessedAtProperty? LastAccessedAt => (folder as ILastAccessedAt)?.LastAccessedAt;

        /// <summary>
        /// Gets the <see cref="ILastAccessedAtOffsetProperty"/> from the item, or null if not supported.
        /// </summary>
        public ILastAccessedAtOffsetProperty? LastAccessedAtOffset => (folder as ILastAccessedAtOffset)?.LastAccessedAtOffset;
    }
}
