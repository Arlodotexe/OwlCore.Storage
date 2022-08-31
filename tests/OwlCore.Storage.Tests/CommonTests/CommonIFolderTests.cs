namespace OwlCore.Storage.Tests.SystemIO;

public abstract class CommonIFolderTests
{
    /// <summary>
    /// Call the constructor using valid input parameters.
    /// </summary>
    public abstract Task<IFolder> CreateFolder();

    /// <summary>
    /// Call the constructor using invalid input parameters, such as a wrong path.
    /// </summary>
    public abstract Task<IFolder> CreateFolderUsingInvalidParameters();

    /// <summary>
    /// Call the constructor using invalid input parameters, such as a wrong path.
    /// </summary>
    public abstract Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount);

    [TestMethod]
    public Task ConstructorCall_ValidParameters()
    {
        // Shouldn't throw when constructor is called.
        return CreateFolder();
    }

    [TestMethod]
    public async Task ConstructorCall_InvalidParameters()
    {
        // Should throw any exception when constructor is called with invalid params.
        // The specific exceptions that can be thrown by the constructor (and ONLY the constructor)
        // are decided by the implementation, not the interface.
        var thrown = false;

        try
        {
            await CreateFolderUsingInvalidParameters();
        }
        catch (Exception)
        {
            thrown = true;
        }
        finally
        {
            Assert.IsTrue(thrown);
        }
    }

    [TestMethod]
    public async Task HasValidName()
    {
        var folder = await CreateFolder();

        Assert.IsFalse(string.IsNullOrWhiteSpace(folder.Name));
    }

    [TestMethod]
    public async Task HasValidId()
    {
        var folder = await CreateFolder();

        Assert.IsFalse(string.IsNullOrWhiteSpace(folder.Id));
        Assert.AreNotEqual(folder.Name, folder.Id, "Names should not be used as an unique identifier. Use something more specific.");
    }

    [TestMethod]
    [DataRow(StorableType.None, 0, 0)]
    [DataRow(StorableType.None, 2, 2)]

    [DataRow(StorableType.File, 2, 0),
     DataRow(StorableType.File, 0, 2),
     DataRow(StorableType.File, 0, 0)]

    [DataRow(StorableType.Folder, 2, 0),
     DataRow(StorableType.Folder, 0, 2),
     DataRow(StorableType.Folder, 0, 0)]

    [DataRow(StorableType.Folder | StorableType.File, 2, 0),
     DataRow(StorableType.Folder | StorableType.File, 0, 2),
     DataRow(StorableType.Folder | StorableType.File, 0, 0)]

    [DataRow(StorableType.All, 2, 0),
     DataRow(StorableType.All, 0, 2),
     DataRow(StorableType.All, 0, 0)]
    public async Task GetItemsAsync_AllCombinations(StorableType type, int fileCount, int folderCount)
    {
        var file = await CreateFolderWithItems(fileCount, folderCount);

        if (type == StorableType.None)
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
            {
                await foreach (var _ in file.GetItemsAsync(type)) { }
            });
            return;
        }

        var returnedFileCount = 0;
        var returnedFolderCount = 0;
        var otherReturnedItemCount = 0;

        await foreach (var item in file.GetItemsAsync(type))
        {
            if (item is IFile)
                returnedFileCount++;
            else if (item is IFolder)
                returnedFolderCount++;
            else
                otherReturnedItemCount++;
        }

        if (type.HasFlag(StorableType.File))
            Assert.AreEqual(fileCount, returnedFileCount, "Incorrect number of files were returned.");

        if (type.HasFlag(StorableType.Folder))
            Assert.AreEqual(folderCount, returnedFolderCount, "Incorrect number of folders were returned.");

        Assert.AreEqual(0, otherReturnedItemCount, "Unknown object types were returned.");
    }

    [TestMethod]
    [AllEnumFlagCombinations(typeof(StorableType))]
    public async Task GetItemsAsync_AllCombinations_ImmediateTokenCancellation(StorableType type)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var folder = await CreateFolder();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await folder.GetItemsAsync(type, cancellationTokenSource.Token).ToListAsync(cancellationToken: cancellationTokenSource.Token), "Does not cancel immediately if a canceled token is passed.");
    }

    [TestMethod]
    [DataRow(StorableType.None, 0, 0)]
    [DataRow(StorableType.None, 2, 2)]

    [DataRow(StorableType.File, 2, 0)]

    [DataRow(StorableType.Folder, 0, 2)]

    [DataRow(StorableType.Folder | StorableType.File, 2, 0),
     DataRow(StorableType.Folder | StorableType.File, 0, 2)]

    [DataRow(StorableType.All, 2, 0),
     DataRow(StorableType.All, 0, 2)]
    public async Task GetItemsAsync_AllCombinations_TokenCancellationDuringEnumeration(StorableType type, int fileCount, int folderCount)
    {
        // No enumeration should take place if set to "None". Tests for this covered elsewhere.
        if (type == StorableType.None)
            return;

        var cancellationTokenSource = new CancellationTokenSource();
        var folder = await CreateFolderWithItems(fileCount, folderCount);

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
        {
            var index = 0;
            await foreach (var item in folder.GetItemsAsync(type, cancellationTokenSource.Token))
            {
                Assert.IsNotNull(item);

                index++;
                if (index > fileCount || index > folderCount)
                {
                    cancellationTokenSource.Cancel();
                }
            }
        });
    }
}