using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory
{
    /// <summary>
    /// A file implementation that resides in memory.
    /// </summary>
    public class MemoryFile : IChildFile
    {
        private readonly MemoryStream _memoryStream;

        /// <summary>
        /// Creates a new instance of <see cref="MemoryFile"/>.
        /// </summary>
        /// <param name="memoryStream">An existing stream which is provided as the file contents.</param>
        public MemoryFile(MemoryStream memoryStream)
            : this($"{memoryStream.GetHashCode()}", $"{memoryStream.GetHashCode()}", memoryStream)
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
        public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult<IFolder?>(Parent);

        /// <inheritdoc />
        public virtual Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (accessMode == 0)
                throw new ArgumentOutOfRangeException(nameof(accessMode), $"{nameof(FileAccess)}.{accessMode} is not valid here.");

            _memoryStream.Position = 0;
            return Task.FromResult<Stream>(new NonDisposableStreamWrapper(_memoryStream));
        }
    }
}
