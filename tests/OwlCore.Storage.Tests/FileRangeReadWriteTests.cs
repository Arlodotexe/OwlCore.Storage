using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests;

[TestClass]
public class FileRangeReadWriteTests
{
        private static Task<IModifiableFolder> CreateRootAsync()
                => Task.FromResult((IModifiableFolder)new MemoryFolder("root-id", "root"));

    [TestMethod]
    public async Task Read_Lines_Range_AllColumns()
    {
        var root = await CreateRootAsync();
        var file = (IFile)await root.CreateFileAsync("a.txt");

        var content = string.Join("\n", new[] { "L0", "L1", "L2", "L3", "L4" });
        await file.WriteTextAsync(content, Encoding.UTF8);

            var lines = await file.ReadTextAsync((1, 4), CancellationToken.None).ToListAsync();
        CollectionAssert.AreEqual(new[] { "L1", "L2", "L3" }, lines);
    }

    [TestMethod]
    public async Task Read_Lines_And_Columns_Range()
    {
        var root = await CreateRootAsync();
        var file = (IFile)await root.CreateFileAsync("a.txt");

        var content = string.Join("\n", new[] { "abcd", "efgh", "ijkl", "mnop" });
        await file.WriteTextAsync(content, Encoding.UTF8);

            var lines = await file.ReadTextAsync((1, 3), (1, 3), CancellationToken.None).ToListAsync();
        CollectionAssert.AreEqual(new[] { "fg", "jk" }, lines);
    }

    [TestMethod]
    public async Task Write_Lines_Range_AllColumns()
    {
    var root = await CreateRootAsync();
    var dst = (IFile)await root.CreateFileAsync("dst.txt");

    var content = string.Join("\n", new[] { "A0", "A1", "A2", "A3" });
    await dst.WriteTextAsync(content, (1, 3), CancellationToken.None);
        var output = await dst.ReadTextAsync();
        Assert.AreEqual("A1\nA2\n", output.Replace("\r\n", "\n"));
    }

    [TestMethod]
    public async Task Write_Lines_And_Columns_Range()
    {
    var root = await CreateRootAsync();
    var dst = (IFile)await root.CreateFileAsync("dst.txt");

    var content = string.Join("\n", new[] { "abcd", "efgh", "ijkl", "mnop" });
    await dst.WriteTextAsync(content, (1, 4), (1, 3), CancellationToken.None);
        var lines = await dst.ReadTextAsync().ConfigureAwait(false);
    Assert.AreEqual("fg\njk\nno\n", lines.Replace("\r\n", "\n"));
    }
}
