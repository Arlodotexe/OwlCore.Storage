using OwlCore.Storage.CommonTests;

namespace OwlCore.Storage.Tests
{
    [TestClass]
    public class HttpFileTests : CommonIFileTests
    {
        public override bool SupportsWriting => false;

        // Required for base class to perform common tests.
        public override Task<IFile> CreateFileAsync()
        {
            var file = new HttpFile(new Uri("https://example.com/"));

            return Task.FromResult<IFile>(file);
        }
    }
}