using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests.SystemIO;

[TestClass]
public class IFolderTests : CommonIModifiableFolderTests
{
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        var directoryInfo = Directory.CreateDirectory(Path.GetTempPath());

        return Task.FromResult<IModifiableFolder>(new SystemFolder(directoryInfo.FullName));
    }

    public override Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(tempFolder);

        for (var i = 0; i < fileCount; i++)
        {
            var path = Path.Combine(tempFolder, $"File.{i}.tmp");
            var file = File.Create(path);
            file.Dispose();
        }

        for (var i = 0; i < folderCount; i++)
        {
            var path = Path.Combine(tempFolder, $"Folder.{i}");
            Directory.CreateDirectory(path);
        }

        return Task.FromResult<IModifiableFolder>(new SystemFolder(tempFolder));
    }

    // Folder Watcher tests
    // TODO: Move these to CommonTests.

    [TestMethod]
    public async Task FolderWatcherOnFileCreate()
    {
        var folder = await CreateModifiableFolderAsync();

        await using var watcher = await folder.GetFolderWatcherAsync();
        var collectionChangedTaskCompletionSource = new TaskCompletionSource();
        watcher.CollectionChanged += (sender, args) => collectionChangedTaskCompletionSource.SetResult();
        
        await folder.CreateFileAsync(GetHashCode().ToString(), overwrite: true);
        
        await collectionChangedTaskCompletionSource.Task;
    }

    [TestMethod]
    [Timeout(2000)]
    public async Task FolderWatcherOnFolderCreate()
    {
        var folder = await CreateModifiableFolderAsync();

        await using var watcher = await folder.GetFolderWatcherAsync();
        var collectionChangedTaskCompletionSource = new TaskCompletionSource();
        watcher.CollectionChanged += (sender, args) => collectionChangedTaskCompletionSource.SetResult();

        await folder.CreateFolderAsync(GetHashCode().ToString(), overwrite: true);
        
        await collectionChangedTaskCompletionSource.Task;
    }

    [TestMethod]
    public async Task FolderWatcherOnDelete()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var existingItem = await folder.GetItemsAsync(StorableType.File).FirstAsync();

        await using var watcher = await folder.GetFolderWatcherAsync();
        var collectionChangedTaskCompletionSource = new TaskCompletionSource();
        watcher.CollectionChanged += (sender, args) => collectionChangedTaskCompletionSource.SetResult();

        await folder.DeleteAsync(existingItem);
        
        await collectionChangedTaskCompletionSource.Task;
    }
}