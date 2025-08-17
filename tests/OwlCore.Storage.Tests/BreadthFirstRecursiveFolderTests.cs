using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests;

[TestClass]
public class BreadthFirstRecursiveFolderTests
{
    private static async Task<MemoryFolder> BuildSampleTreeAsync()
    {
        // root
        var root = new MemoryFolder("root-id", "root");

        // root items (in insertion order)
        await root.CreateFileAsync("root-file");            // file
        var a = (IModifiableFolder)await root.CreateFolderAsync("A"); // folder A
        var b = (IModifiableFolder)await root.CreateFolderAsync("B"); // folder B

        // A's children
        await a.CreateFileAsync("A1");                      // file
        var aa = (IModifiableFolder)await a.CreateFolderAsync("AA");   // folder
        await a.CreateFolderAsync("AB");                    // empty folder

        // AA's child
        await aa.CreateFileAsync("AA1");                    // file

        // B's child
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
    public async Task Enumerates_BreadthFirst_All()
    {
        var root = await BuildSampleTreeAsync();
        var bfs = new BreadthFirstRecursiveFolder(root);

        var items = await CollectAsync(bfs.GetItemsAsync(StorableType.All));
        var names = items.Select(x => x.Name).ToArray();

        // Expected breadth-first traversal order (files and folders)
        CollectionAssert.AreEqual(new[]
        {
            // Depth 1 (root's direct children): root-file, A, B
            "root-file",
            "A",
            "B",
            // Depth 2: A1, AA, AB, B1
            "A1",
            "AA",
            "AB",
            "B1",
            // Depth 3: AA1
            "AA1",
        }, names);
    }

    [TestMethod]
    public async Task Enumerates_BreadthFirst_Files_Only()
    {
        var root = await BuildSampleTreeAsync();
        var bfs = new BreadthFirstRecursiveFolder(root);

        var items = await CollectAsync(bfs.GetItemsAsync(StorableType.File));
        var names = items.Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            // Depth 1
            "root-file",
            // Depth 2
            "A1",
            "B1",
            // Depth 3
            "AA1",
        }, names);
    }

    [TestMethod]
    public async Task Enumerates_BreadthFirst_Folders_Only()
    {
        var root = await BuildSampleTreeAsync();
        var bfs = new BreadthFirstRecursiveFolder(root);

        var items = await CollectAsync(bfs.GetItemsAsync(StorableType.Folder));
        var names = items.Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            // Depth 1
            "A",
            "B",
            // Depth 2
            "AA",
            "AB",
        }, names);
    }

    [TestMethod]
    public async Task Respects_MaxDepth_Level1()
    {
        var root = await BuildSampleTreeAsync();
        var bfs = new BreadthFirstRecursiveFolder(root)
        {
            MaxDepth = 1,
        };

        var items = await CollectAsync(bfs.GetItemsAsync(StorableType.All));
        var names = items.Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            // Only direct children of root
            "root-file",
            "A",
            "B",
        }, names);
    }

    [TestMethod]
    public async Task Respects_MaxDepth_Level2()
    {
        var root = await BuildSampleTreeAsync();
        var bfs = new BreadthFirstRecursiveFolder(root)
        {
            MaxDepth = 2,
        };

        var items = await CollectAsync(bfs.GetItemsAsync(StorableType.All));
        var names = items.Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            // Depth 1
            "root-file",
            "A",
            "B",
            // Depth 2
            "A1",
            "AA",
            "AB",
            "B1",
        }, names);
    }
}
