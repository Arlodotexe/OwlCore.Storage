using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests.SystemIO;

[TestClass]
public class SystemFolderTests : CommonIModifiableFolderTests
{
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var directoryInfo = Directory.CreateDirectory(tempFolder);

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

    public override Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);
        Directory.SetCreationTimeUtc(tempFolder, createdAt);
        return Task.FromResult<IFolder?>(new SystemFolder(tempFolder));
    }

    public override Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);
        Directory.SetLastWriteTimeUtc(tempFolder, lastModifiedAt);
        return Task.FromResult<IFolder?>(new SystemFolder(tempFolder));
    }

    public override Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);
        Directory.SetLastAccessTimeUtc(tempFolder, lastAccessedAt);
        return Task.FromResult<IFolder?>(new SystemFolder(tempFolder));
    }

    public override Task<IFile?> CreateFileInFolderWithLastModifiedAtAsync(IModifiableFolder folder, DateTime lastModifiedAt)
    {
        if (folder is not SystemFolder systemFolder)
            return Task.FromResult<IFile?>(null);
            
        var filePath = Path.Combine(systemFolder.Path, $"{Guid.NewGuid()}.tmp");
        using (var fs = File.Create(filePath))
        {
            // Write some content
            var data = new byte[1024];
            new Random().NextBytes(data);
            fs.Write(data);
        }
        
        File.SetLastWriteTimeUtc(filePath, lastModifiedAt);
        return Task.FromResult<IFile?>(new SystemFile(filePath));
    }

    public override Task<IFile?> CreateFileInFolderWithTimestampsAsync(IModifiableFolder folder, DateTime? createdAt, DateTime? lastModifiedAt, DateTime? lastAccessedAt)
    {
        if (folder is not SystemFolder systemFolder)
            return Task.FromResult<IFile?>(null);
            
        var filePath = Path.Combine(systemFolder.Path, $"{Guid.NewGuid()}.tmp");
        using (var fs = File.Create(filePath))
        {
            // Write some content
            var data = new byte[1024];
            new Random().NextBytes(data);
            fs.Write(data);
        }
        
        if (createdAt.HasValue)
            File.SetCreationTimeUtc(filePath, createdAt.Value);
        if (lastModifiedAt.HasValue)
            File.SetLastWriteTimeUtc(filePath, lastModifiedAt.Value);
        if (lastAccessedAt.HasValue)
            File.SetLastAccessTimeUtc(filePath, lastAccessedAt.Value);
            
        return Task.FromResult<IFile?>(new SystemFile(filePath));
    }

    // Folder Watcher tests
    // TODO: Move these to CommonTests.

    [TestMethod]
    [Timeout(2000)]
    public async Task FolderWatcherOnFileCreate()
    {
        var folder = await CreateModifiableFolderAsync();

        await using var watcher = await folder.GetFolderWatcherAsync();
        var collectionChangedTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.CollectionChanged += (sender, args) => collectionChangedTaskCompletionSource.TrySetResult();

        var fileName = $"{Guid.NewGuid():N}.tmp";
        await folder.CreateFileAsync(fileName, overwrite: true);

        await collectionChangedTaskCompletionSource.Task;
    }

    [TestMethod]
    [Timeout(2000)]
    public async Task FolderWatcherOnFolderCreate()
    {
        var folder = await CreateModifiableFolderAsync();

        await using var watcher = await folder.GetFolderWatcherAsync();
        var collectionChangedTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.CollectionChanged += (sender, args) => collectionChangedTaskCompletionSource.TrySetResult();

        var folderName = Guid.NewGuid().ToString("N");
        await folder.CreateFolderAsync(folderName, overwrite: true);

        await collectionChangedTaskCompletionSource.Task;
    }

    [TestMethod]
    [Timeout(2000)]
    public async Task FolderWatcherOnDelete()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var existingItem = await folder.GetItemsAsync(StorableType.File).FirstAsync();

        await using var watcher = await folder.GetFolderWatcherAsync();
        var collectionChangedTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.CollectionChanged += (sender, args) => collectionChangedTaskCompletionSource.TrySetResult();

        await folder.DeleteAsync(existingItem);

        await collectionChangedTaskCompletionSource.Task;
    }
}
