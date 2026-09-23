using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory
{
    /// <summary>
    /// A file implementation that resides in memory.
    /// </summary>
    public class MemoryFile : IChildFile, ICreatedAtOffset, ILastModifiedAtOffset, ILastAccessedAtOffset
    {
        private readonly MemoryStream _memoryStream;

        /// <summary>
        /// Creates a new instance of <see cref="MemoryFile"/> with same new GUID as ID and Name.
        /// </summary>
        /// <param name="memoryStream">An existing stream which is provided as the file contents.</param>
        public MemoryFile(MemoryStream memoryStream) : this(memoryStream, Guid.NewGuid().ToString())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryFile"/> with the given <paramref name="name"/> as both Name and its hash code as Id.
        /// </summary>
        /// <param name="memoryStream">An existing stream which is provided as the file contents.</param>
        /// <param name="name">Used to assign both Name and its hash code as Id.</param>
        public MemoryFile(MemoryStream memoryStream, string name) : this($"{name.GetHashCode()}", name, memoryStream)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryFile"/>.
        /// </summary>
        /// <param name="memoryStream">An existing stream which is provided as the file contents.</param>
        /// <param name="id">A unique and consistent identifier for this file or folder.</param>
        /// <param name="name">The name of the file or folder, with the extension (if any).</param>
        public MemoryFile(MemoryStream memoryStream, string id, string name) : this(id, name, memoryStream)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="MemoryFile"/>.
        /// </summary>
        /// <param name="id">A unique and consistent identifier for this file or folder.</param>
        /// <param name="name">The name of the file or folder, with the extension (if any).</param>
        /// <param name="memoryStream">An existing stream which is provided as the file contents.</param>
        public MemoryFile(string id, string name, MemoryStream memoryStream)
        {
            _memoryStream = memoryStream;
            Id = id;
            Name = name;

            CreatedAtOffset = new MemoryCreatedAtOffsetProperty(this, new() { Value = DateTimeOffset.Now });
            CreatedAt = new MemoryCreatedAtProperty(this, new() { Value = DateTime.Now });

            LastModifiedAt = new MemoryLastModifiedAtProperty(this, new() { Value = null });
            LastModifiedAtOffset = new MemoryLastModifiedAtOffsetProperty(this, new() { Value = null });

            LastAccessedAt = new MemoryLastAccessedAtProperty(this, new() { Value = null });
            LastAccessedAtOffset = new MemoryLastAccessedAtOffsetProperty(this, new() { Value = null });
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// The parent folder, if any.
        /// </summary>
        public MemoryFolder? Parent { get; protected internal set; }

        /// <inheritdoc />
        public ICreatedAtOffsetProperty CreatedAtOffset { get; init; }

        /// <inheritdoc />
        public ICreatedAtProperty CreatedAt { get; init; }

        /// <inheritdoc />
        public ILastModifiedAtOffsetProperty LastModifiedAtOffset { get; init; }

        /// <inheritdoc />
        public ILastModifiedAtProperty LastModifiedAt { get; init; }

        /// <inheritdoc />
        public ILastAccessedAtOffsetProperty LastAccessedAtOffset { get; init; }

        /// <inheritdoc />
        public ILastAccessedAtProperty LastAccessedAt { get; init; }

        /// <inheritdoc />
        public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(Parent);

        /// <inheritdoc />
        public virtual Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (accessMode == 0)
                throw new ArgumentOutOfRangeException(nameof(accessMode), $"{nameof(FileAccess)}.{accessMode} is not valid here.");

            _memoryStream.Position = 0;
            UpdateLastAccessed();

            var updateLastModifiedOnDisposeWhileSuppressingDisposeOnUnderlyingStream = new DelegatedDisposalStream(new NonDisposableStreamWrapper(_memoryStream))
            {
                Inner = new DisposableDelegate
                {
                    Inner = () =>
                    {
                        if (accessMode.HasFlag(FileAccess.Write))
                            UpdateLastModified();
                    }
                }
            };

            return Task.FromResult<Stream>(updateLastModifiedOnDisposeWhileSuppressingDisposeOnUnderlyingStream);
        }

        private void UpdateLastAccessed()
        {
            var now = DateTimeOffset.Now;
            ((MemoryLastAccessedAtProperty)LastAccessedAt).DateTimeValue.Value = now.LocalDateTime;
            ((MemoryLastAccessedAtOffsetProperty)LastAccessedAtOffset).DateTimeOffsetValue.Value = now;
        }

        private void UpdateLastModified()
        {
            var now = DateTimeOffset.Now;
            ((MemoryLastModifiedAtProperty)LastModifiedAt).DateTimeValue.Value = now.LocalDateTime;
            ((MemoryLastModifiedAtOffsetProperty)LastModifiedAtOffset).DateTimeOffsetValue.Value = now;
        }
    }
}
