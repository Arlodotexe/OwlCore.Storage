using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.Net.Http;

namespace OwlCore.Storage.Tests;

[TestClass]
public class HttpFileTests : CommonIFileTests
{
    public override bool SupportsWriting => false;

    // Required for base class to perform common tests.
    public override Task<IFile> CreateFileAsync()
    {
        // example.com was previously used here
        // but was changed due to slow retrieval times.
        var file = new HttpFile(new Uri("https://httpbin.org/"));

        return Task.FromResult<IFile>(file);
    }

    // HttpFile doesn't support setting timestamps
    public override Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt) => Task.FromResult<IFile?>(null);
    public override Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt) => Task.FromResult<IFile?>(null);
    public override Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt) => Task.FromResult<IFile?>(null);

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
