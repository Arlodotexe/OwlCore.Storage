using OwlCore.Storage.CommonTests;

namespace OwlCore.Storage.Tests
{
    [TestClass]
    public class StreamFileTests : CommonIFileTests
    {
        // Required for base class to perform common tests.
        public override Task<IFile> CreateFileAsync()
        {
            var randomData = GenerateRandomData(256_000);
            var memoryStream = new MemoryStream(randomData);
            
            var file = new StreamFile(memoryStream);

            return Task.FromResult<IFile>(file);

            static byte[] GenerateRandomData(int length)
            {
                var rand = new Random();
                var b = new byte[length];
                rand.NextBytes(b);

                return b;
            }
        }
    }
}