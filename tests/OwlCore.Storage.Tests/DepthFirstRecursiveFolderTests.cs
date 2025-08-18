using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests;

[TestClass]
public class DepthFirstRecursiveFolderTests
{
    private static async Task<MemoryFolder> BuildSampleTreeAsync()
    {
        // root
        var root = new MemoryFolder("root-id", "root");

        // root items (in insertion order)
        await root.CreateFileAsync("root-file");            // file
        var a = (IModifiableFolder)await root.CreateFolderAsync("A"); // folder A

        // A's children
        await a.CreateFileAsync("A1");                      // file
        var aa = (IModifiableFolder)await a.CreateFolderAsync("AA");   // folder
        await aa.CreateFileAsync("AA1");                    // file
        await a.CreateFolderAsync("AB");                    // empty folder

        var b = (IModifiableFolder)await root.CreateFolderAsync("B");  // folder B
        await b.CreateFileAsync("B1");                      // file

        return root;
    }

    private static async Task<List<IStorableChild>> CollectAsync(IAsyncEnumerable<IStorableChild> items)
    {
        var list = new List<IStorableChild>();
        await foreach (var item in items)
            list.Add(item);
        return list;
    }

    [TestMethod]
    public async Task Enumerates_DepthFirst_All()
    {
        var root = await BuildSampleTreeAsync();
        var dfs = new DepthFirstRecursiveFolder(root);

        var items = await CollectAsync(dfs.GetItemsAsync(StorableType.All));
        var names = items.Select(x => x.Name).ToArray();

        // Expected depth-first traversal order (files and folders)
        CollectionAssert.AreEqual(new[]
        {
            "root-file",
            "A",
            "A1",
            "AA",
            "AA1",
            "AB",
            "B",
            "B1",
        }, names);
    }

    [TestMethod]
    public async Task Enumerates_Files_Only()
    {
        var root = await BuildSampleTreeAsync();
        var dfs = new DepthFirstRecursiveFolder(root);

        var items = await CollectAsync(dfs.GetItemsAsync(StorableType.File));
        var names = items.Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            "root-file",
            "A1",
            "AA1",
            "B1",
        }, names);
    }

    [TestMethod]
    public async Task Enumerates_Folders_Only()
    {
        var root = await BuildSampleTreeAsync();
        var dfs = new DepthFirstRecursiveFolder(root);

        var items = await CollectAsync(dfs.GetItemsAsync(StorableType.Folder));
        var names = items.Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            "A",
            "AA",
            "AB",
            "B",
        }, names);
    }

    [TestMethod]
    public async Task Respects_MaxDepth_Level1()
    {
        var root = await BuildSampleTreeAsync();
        var dfs = new DepthFirstRecursiveFolder(root)
        {
            MaxDepth = 1,
        };

        var items = await CollectAsync(dfs.GetItemsAsync(StorableType.All));
        var names = items.Select(x => x.Name).ToArray();

        // Depth = 1: only root-level items, no traversal into child folders
        CollectionAssert.AreEqual(new[]
        {
            "root-file",
            "A",
            "B",
        }, names);
    }

    [TestMethod]
    public async Task Respects_MaxDepth_Level2()
    {
        var root = await BuildSampleTreeAsync();
        var dfs = new DepthFirstRecursiveFolder(root)
        {
            MaxDepth = 2,
        };

        var items = await CollectAsync(dfs.GetItemsAsync(StorableType.All));
        var names = items.Select(x => x.Name).ToArray();

        // Depth = 2: includes items within first-level folders, but does not traverse into AA
        CollectionAssert.AreEqual(new[]
        {
            "root-file",
            "A",
            "A1",
            "AA",
            "AB",
            "B",
            "B1",
        }, names);
    }
}
