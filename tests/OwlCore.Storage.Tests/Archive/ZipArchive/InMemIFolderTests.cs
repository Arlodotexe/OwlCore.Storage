using OwlCore.Storage.CommonTests;
using System.IO.Compression;
using OwlCore.Storage.Memory;
using OwlCore.Storage.System.IO.Compression;

namespace OwlCore.Storage.Tests.Archive.ZipArchive;

/// <summary>
/// A test suite for <see cref="ZipArchiveFolder"/>s created entirely in memory.
/// </summary>
[TestClass]
public class InMemIFolderTests : CommonIModifiableFolderTests
{
    // Required for base class to perform common tests.
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        var archiveStream = new MemoryStream(256);
        var sourceFile = new MemoryFile($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", archiveStream);

        return Task.FromResult<IModifiableFolder>(new ZipArchiveFolder(sourceFile));
    }

    public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
    {
        var folder = await CreateModifiableFolderAsync();

        for (int i = 0; i < fileCount; i++)
        {
            await folder.CreateFileAsync($"{Guid.NewGuid()}");
        }

        for (int i = 0; i < folderCount; i++)
        {
            await folder.CreateFolderAsync($"{Guid.NewGuid()}");
        }

        return folder;
    }

    public async Task<IModifiableFolder> CreateModifiableFolderWithNestedItems()
    {
        // Create the following directory structure:
        // root
        //  ├ subA
        //  │  └ fileB
        //  ├ subB
        //  │  ├ subC
        //  │  │  └ fileC
        //  │  └ fileB
        //  └ fileRoot

        var root = await CreateModifiableFolderAsync();
        {
            var subA = await root.CreateFolderAsync("subA") as IModifiableFolder;
            {
                Assert.IsNotNull(subA);

                await subA.CreateFileAsync("fileA");
            }

            var subB = await root.CreateFolderAsync("subB") as IModifiableFolder;
            {
                Assert.IsNotNull(subB);

                await subB.CreateFileAsync("fileB");

                var subC = await subB.CreateFolderAsync("subC") as IModifiableFolder;
                {
                    Assert.IsNotNull(subC);

                    await subC.CreateFileAsync("fileC");
                }
            }

            await root.CreateFileAsync("fileRoot");
        }

        return root;
    }

    [TestMethod]
    public async Task CreateNewFolderAsyncTest_FolderWithNestedItems()
    {
        var root = await CreateModifiableFolderWithNestedItems();

        // Check each path
        var fileRootEx = await root.GetItemAsync($"{root.Id}fileRoot");
        Assert.AreEqual("fileRoot", fileRootEx.Name);

        var subAEx = await root.GetItemAsync($"{root.Id}subA/") as IChildFolder;
        Assert.IsNotNull(subAEx);
        Assert.AreEqual("subA", subAEx.Name);

        var fileAEx = await subAEx.GetItemAsync($"{root.Id}subA/fileA");
        Assert.IsInstanceOfType<IFile>(fileAEx);
        Assert.AreEqual("fileA", fileAEx.Name);

        var subBEx = await root.GetItemAsync($"{root.Id}subB/") as IChildFolder;
        Assert.IsNotNull(subBEx);
        Assert.AreEqual("subB", subBEx.Name);

        var fileBEx = await subBEx.GetItemAsync($"{root.Id}subB/fileB");
        Assert.IsInstanceOfType<IFile>(fileBEx);
        Assert.AreEqual("fileB", fileBEx.Name);

        var subCEx = await subBEx.GetItemAsync($"{root.Id}subB/subC/") as IChildFolder;
        Assert.IsNotNull(subCEx);
        Assert.AreEqual("subC", subCEx.Name);

        var fileCEx = await subCEx.GetItemAsync($"{root.Id}subB/subC/fileC");
        Assert.IsInstanceOfType<IFile>(fileCEx);
        Assert.AreEqual("fileC", fileCEx.Name);
    }

    [TestMethod]
    public async Task GetItemsAsyncText_FolderWithNestedItems()
    {
        var root = await CreateModifiableFolderWithNestedItems();

        var subA = await root.GetItemAsync($"{root.Id}subA/") as IFolder;
        {
            Assert.IsNotNull(subA);

            var subAItems = await subA.GetItemsAsync().ToListAsync();
            Assert.AreEqual(1, subAItems.Count);

            var fileA = subAItems[0];
            Assert.IsInstanceOfType<IFile>(fileA);
            Assert.AreEqual($"{root.Id}subA/fileA", fileA.Id);
        }

        var subB = await root.GetItemAsync($"{root.Id}subB/") as IFolder;
        {
            Assert.IsNotNull(subB);

            var subBItems = await subB.GetItemsAsync().ToListAsync();
            Assert.AreEqual(2, subBItems.Count);

            var fileB = subBItems.First(i => i.Id == $"{root.Id}subB/fileB");
            Assert.IsInstanceOfType<IFile>(fileB);

            var subC = subBItems.First(i => i.Id == $"{root.Id}subB/subC/") as IFolder;
            {
                Assert.IsNotNull(subC);

                var subCItems = await subC.GetItemsAsync().ToListAsync();
                Assert.AreEqual(1, subCItems.Count);

                var fileC = subCItems[0];
                Assert.IsInstanceOfType<IFile>(fileC);
                Assert.AreEqual($"{root.Id}subB/subC/fileC", fileC.Id);
            }
        }
    }
}