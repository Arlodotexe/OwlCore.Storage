using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests.Memory
{
    [TestClass]
    public class MemoryFileTests : CommonIFileTests
    {
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
        public override Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt) => Task.FromResult<IFile?>(null);
        public override Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt) => Task.FromResult<IFile?>(null);
        public override Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt) => Task.FromResult<IFile?>(null);
    }
}
