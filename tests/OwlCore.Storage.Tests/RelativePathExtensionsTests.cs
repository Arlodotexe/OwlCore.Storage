using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests;

[TestClass]
public class RelativePathExtensionsTests
{
    private readonly MemoryFolder _rootFolder = new(id: Guid.Empty.ToString(), name: "root");

    [TestInitialize]
    public async Task SetupAsync()
    {
        var a = (MemoryFolder)await _rootFolder.CreateFolderAsync("a");
        var b = (MemoryFolder)await _rootFolder.CreateFolderAsync("b");
        var c = (MemoryFile)await _rootFolder.CreateFileAsync("c");

        var aa = (MemoryFolder)await a.CreateFolderAsync("a");
        var ab = (MemoryFolder)await a.CreateFolderAsync("b");
        var ac = (MemoryFile)await a.CreateFileAsync("c");

        var ba = (MemoryFolder)await b.CreateFolderAsync("a");
        var bb = (MemoryFolder)await b.CreateFolderAsync("b");
        var bc = (MemoryFile)await b.CreateFileAsync("c");

        var aaa = (MemoryFolder)await aa.CreateFolderAsync("a");
        var aab = (MemoryFolder)await aa.CreateFolderAsync("b");
        var aac = (MemoryFile)await aa.CreateFileAsync("c");

        var baa = (MemoryFolder)await ba.CreateFolderAsync("a");
        var bab = (MemoryFolder)await ba.CreateFolderAsync("b");
        var bac = (MemoryFile)await ba.CreateFileAsync("c");

        var bba = (MemoryFolder)await bb.CreateFolderAsync("a");
        var bbb = (MemoryFolder)await bb.CreateFolderAsync("b");
        var bbc = (MemoryFile)await bb.CreateFileAsync("c");
    }

    [
        DataRow("/"),

        DataRow("/a/"),
        DataRow("/b/"),
        DataRow("/c"),

        DataRow("/a/a/"),
        DataRow("/a/b/"),
        DataRow("/a/c"),

        DataRow("/a/a/a/"),
        DataRow("/a/a/b/"),
        DataRow("/a/a/c"),

        DataRow("/b/a/"),
        DataRow("/b/b/"),
        DataRow("/b/c"),

        DataRow("/b/a/a/"),
        DataRow("/b/a/b/"),
        DataRow("/b/a/c"),
    ]
    [TestMethod]
    public async Task TraverseAndRegenerateRelativePath(string relativePath)
    {
        // Traverse to relative path
        var item = (IStorableChild)await _rootFolder.GetItemByRelativePathAsync(relativePath);

        if (relativePath == "/")
            Assert.AreEqual(_rootFolder, item);
        else
            Assert.IsTrue(relativePath.Contains(item.Name));

        // Generate new relative path
        var newRelativePath = await _rootFolder.GetRelativePathToAsync(item);

        // Make sure they match
        Assert.AreEqual(relativePath, newRelativePath);
    }
}