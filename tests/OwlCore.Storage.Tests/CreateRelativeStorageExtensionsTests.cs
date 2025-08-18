using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests;

[TestClass]
public class CreateRelativeStorageExtensionsTests
{
    private MemoryFolder _root = null!;

    [TestInitialize]
    public async Task InitAsync()
    {
        _root = new MemoryFolder(id: Guid.NewGuid().ToString(), name: "root");

        // Minimal, shared structure used across tests
        var folderA = (MemoryFolder)await _root.CreateFolderAsync("folderA");
        await folderA.CreateFolderAsync("subA");
        await folderA.CreateFileAsync("fileA.txt");
        await _root.CreateFileAsync("fileRoot.txt");
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Ensure no cross-test state is retained
        _root = null!;
    }

    [TestMethod]
    public async Task CreateRelativeFolder_StartingFromFolder_StorableApi()
    {
        // Arrange
        var start = _root;

        // Act
        var final = await start.CreateFolderByRelativePathAsync("folderA/subB");

        // Assert
        Assert.AreEqual("subB", final.Name);
        var parent = await final.GetParentAsync();
        Assert.IsNotNull(parent);
        Assert.AreEqual("folderA", parent!.Name);
    }

    [TestMethod]
    public async Task CreateRelativeFolders_YieldsInOrder_IgnoresFileLikeTail()
    {
        // Arrange
        var yielded = new List<string>();

        // Act
        await foreach (var f in _root.CreateFoldersAlongRelativePathAsync("folderA/subC/new.txt"))
            yielded.Add(f.Name);

        // Assert: should create folderA (existing), subC (created), and ignore new.txt
        CollectionAssert.AreEqual(new[] { "folderA", "subC" }, yielded);
    }

    [TestMethod]
    public async Task CreateRelativeFolder_FromFile_TraversesUpWithDotDot()
    {
        // Arrange: start from file root/fileRoot.txt
        var file = (IChildFile)await _root.GetFirstByNameAsync("fileRoot.txt");

        // Act: go up to root, then create chain under it
        var final = await file.CreateFolderByRelativePathAsync("../created/chain");

        // Assert
        Assert.AreEqual("chain", final.Name);
        var parent = await final.GetParentAsync();
        Assert.IsNotNull(parent);
        Assert.AreEqual("created", parent!.Name);
    }

    [TestMethod]
    public async Task CreateByRelativePath_CreatesFile_AndParents()
    {
        // Arrange: start from root folder
        var start = _root;

        // Act
        var file = await start.CreateFileByRelativePathAsync("nested/path/newfile.txt");

        // Assert
        Assert.AreEqual("newfile.txt", file.Name);
        var parent = await file.GetParentAsync();
        Assert.IsNotNull(parent);
        Assert.AreEqual("path", parent!.Name);
    }

    [TestMethod]
    public async Task CreateByRelativePath_CreatesFolder_Explicit()
    {
        // Arrange
        var start = _root;

        // Act
        var folder = await start.CreateFolderByRelativePathAsync("x/y/z");

        // Assert
        Assert.AreEqual("z", folder.Name);
        var parent = await folder.GetParentAsync();
        Assert.IsNotNull(parent);
        Assert.AreEqual("y", parent!.Name);
    }

    [TestMethod]
    public async Task CreateByRelativePath_FileRejectsTrailingSlash()
    {
        // Arrange
        var start = _root;

        // Act/Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            await start.CreateFileByRelativePathAsync("a/b/c/"));
    }

    [TestMethod]
    public async Task CreateAlongRelativePath_YieldsParentsThenFile()
    {
        var start = _root;
        var yielded = new List<string>();

        await foreach (var item in start.CreateAlongRelativePathAsync("p/q/r.txt", StorableType.File))
            yielded.Add(item.Name);

        CollectionAssert.AreEqual(new[] { "p", "q", "r.txt" }, yielded);
    }

    [TestMethod]
    public async Task CreateAlongRelativePath_FolderTarget_YieldsEachFolder_IncludingDotDot()
    {
        // Arrange: start from file and go up, then create under root
        var file = (IChildFile)await _root.GetFirstByNameAsync("fileRoot.txt");
        var yielded = new List<string>();

        await foreach (var item in file.CreateAlongRelativePathAsync("../x/y", StorableType.Folder))
            yielded.Add(item.Name);

        CollectionAssert.AreEqual(new[] { "root", "x", "y" }, yielded);
    }
}
