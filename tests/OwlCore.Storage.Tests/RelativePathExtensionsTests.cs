using OwlCore.Storage.Memory;
using System.Linq;

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

        // Add missing items for complete test coverage
        var abc = (MemoryFile)await ab.CreateFileAsync("c");

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
    public async Task GetRelativePathToItems_YieldsExpectedSequence(string relativePath)
    {
        var target = (IStorableChild)await _rootFolder.GetItemByRelativePathAsync(relativePath);

        var expectedSegments = relativePath == "/"
            ? Array.Empty<string>()
            : relativePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        var yielded = new List<IStorable>();
        await foreach (var item in _rootFolder.GetItemsAlongRelativePathToAsync(target))
        {
            yielded.Add(item);
        }

        Assert.AreEqual(expectedSegments.Length, yielded.Count, "Yielded count should match number of path segments.");

        for (int i = 0; i < expectedSegments.Length; i++)
        {
            Assert.AreEqual(expectedSegments[i], yielded[i].Name, $"Segment {i} name mismatch.");
        }

        if (expectedSegments.Length == 0)
        {
            // Root case: nothing should be yielded.
            Assert.AreEqual(0, yielded.Count);
        }
        else
        {
            // Validate last item type matches file vs folder according to trailing slash.
            if (relativePath.EndsWith("/"))
                Assert.IsInstanceOfType(yielded[^1], typeof(IFolder));
            else
                Assert.IsInstanceOfType(yielded[^1], typeof(IFile));
        }
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

        // Normalization cases
        DataRow("/a/./b/"),
        DataRow("\\a\\b\\c"),
    ]
    [TestMethod]
    public async Task GetItemsAlongRelativePath_YieldsExpectedSequence(string relativePath)
    {
        var normalized = (relativePath ?? string.Empty).Replace('\\', '/');
        var expectedSegments = normalized == "/"
            ? Array.Empty<string>()
            : normalized.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s != ".")
                .ToArray();

        var yielded = new List<IStorable>();
    await foreach (var item in _rootFolder.GetItemsAlongRelativePathAsync(relativePath ?? string.Empty))
        {
            yielded.Add(item);
        }

        Assert.AreEqual(expectedSegments.Length, yielded.Count, "Yielded count should match number of path segments.");

        for (int i = 0; i < expectedSegments.Length; i++)
        {
            Assert.AreEqual(expectedSegments[i], yielded[i].Name, $"Segment {i} name mismatch.");
        }

        if (expectedSegments.Length == 0)
        {
            // Root case: nothing should be yielded.
            Assert.AreEqual(0, yielded.Count);
        }
        else
        {
            // Validate last item type matches file vs folder according to trailing slash of the normalized path.
            if (normalized.EndsWith("/"))
                Assert.IsInstanceOfType(yielded[^1], typeof(IFolder));
            else
                Assert.IsInstanceOfType(yielded[^1], typeof(IFile));
        }
    }

    [TestMethod]
    public async Task GetItemsAlongRelativePath_SupportsParentTraversal()
    {
        // Start at /a/a/
        var start = (IStorableChild)await _rootFolder.GetItemByRelativePathAsync("/a/a/");

        // Go up to /a then to file /a/c
        var yielded = new List<IStorable>();
        await foreach (var item in start.GetItemsAlongRelativePathAsync("../c"))
        {
            yielded.Add(item);
        }

        Assert.AreEqual(2, yielded.Count);
        Assert.AreEqual("a", yielded[0].Name);  // The parent /a/
        Assert.AreEqual("c", yielded[1].Name);  // The target file /a/c
        Assert.IsInstanceOfType(yielded[0], typeof(IFolder));
        Assert.IsInstanceOfType(yielded[1], typeof(IFile));

        // Multiple parents: from /b/a/b/ to ../../c -> /b/c
        start = (IStorableChild)await _rootFolder.GetItemByRelativePathAsync("/b/a/b/");
        yielded.Clear();
        await foreach (var item in start.GetItemsAlongRelativePathAsync("../../c"))
        {
            yielded.Add(item);
        }

        Assert.AreEqual(3, yielded.Count);
        Assert.AreEqual("a", yielded[0].Name);  // First parent: /b/a/
        Assert.AreEqual("b", yielded[1].Name);  // Second parent: /b/
        Assert.AreEqual("c", yielded[2].Name);  // Target file: /b/c
        Assert.IsInstanceOfType(yielded[0], typeof(IFolder));
        Assert.IsInstanceOfType(yielded[1], typeof(IFolder));
        Assert.IsInstanceOfType(yielded[2], typeof(IFile));
    }
}