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

        [TestMethod]
        public void NameShouldNotMatchUri()
        {
            var file = new HttpFile(new Uri("https://example.com/test.zip"));

            Assert.AreNotEqual(file.Name, file.Uri.OriginalString);
            Assert.IsTrue(file.Uri.OriginalString.Contains(file.Name));
        }

        [TestMethod]
        public void FileNameOnSimpleUrl()
        {
            var file = new HttpFile(new Uri("https://example.com/test.zip"));

            Assert.AreNotEqual(file.Name, file.Uri.OriginalString);
            Assert.IsTrue(file.Uri.OriginalString.Contains(file.Name));
            Assert.AreEqual(file.Name, "test.zip");
        }

        [TestMethod]
        public void FileNameOnUrlWithParams()
        {
            var file = new HttpFile(new Uri("https://example.com/test.zip?utm_content=This+Is+\"Broken\""));

            Assert.AreNotEqual(file.Name, file.Uri.OriginalString);
            Assert.IsTrue(file.Uri.OriginalString.Contains(file.Name));
            Assert.AreEqual(file.Name, "test.zip");
        }
    }
}