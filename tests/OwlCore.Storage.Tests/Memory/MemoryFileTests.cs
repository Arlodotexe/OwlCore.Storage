using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests.Memory
{
    [TestClass]
    public class MemoryFileTests : CommonIFileTests
    {
        public override PropertyValueAvailability CreatedAtAvailability => PropertyValueAvailability.Always;
        public override PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Maybe;
        public override PropertyValueAvailability LastModifiedAtAvailability => PropertyValueAvailability.Maybe;

        // Required for base class to perform common tests.
        public override async Task<IFile> CreateFileAsync()
        {
            var randomData = GenerateRandomData(256_000);
            using var tempStr = new MemoryStream(randomData);

            var memoryStream = new MemoryStream();
            await tempStr.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new MemoryFile(memoryStream);

            static byte[] GenerateRandomData(int length)
            {
                var rand = new Random();
                var b = new byte[length];
                rand.NextBytes(b);

                return b;
            }
        }

        // MemoryFile doesn't support setting timestamps
        public override async Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt)
        {
            var file = new MemoryFile(new());

            var created = (MemoryCreatedAtProperty)file.CreatedAt;
            created.DateTimeValue.Value = createdAt;

            var createdOffset = (MemoryCreatedAtOffsetProperty)file.CreatedAtOffset;
            createdOffset.DateTimeOffsetValue.Value = createdAt;

            return file;
        }

        public override async Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt)
        {
            var file = new MemoryFile(new());

            var lastModified = (MemoryLastModifiedAtProperty)file.LastModifiedAt;
            lastModified.DateTimeValue.Value = lastModifiedAt;

            var lastModifiedOffset = (MemoryLastModifiedAtOffsetProperty)file.LastModifiedAtOffset;
            lastModifiedOffset.DateTimeOffsetValue.Value = lastModifiedAt;

            return file;
        }

        public override async Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt)
        {
            var file = new MemoryFile(new());

            var lastAccessed = (MemoryLastAccessedAtProperty)file.LastAccessedAt;
            lastAccessed.DateTimeValue.Value = lastAccessedAt;

            var lastAccessedOffset = (MemoryLastAccessedAtOffsetProperty)file.LastAccessedAtOffset;
            lastAccessedOffset.DateTimeOffsetValue.Value = lastAccessedAt;

            return file;
        }
    }
}
