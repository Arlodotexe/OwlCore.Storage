using System.Runtime.InteropServices;
using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests.SystemIO;

[TestClass]
public class SystemFolderTests : CommonIModifiableFolderTests
{
    // TODO: Check MacOS and FreeBSD behavior and add to conditionals below if needed once support for those platforms is tested and added.
    public override PropertyUpdateBehavior LastModifiedAtUpdateBehavior => true switch
    {
        _ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => PropertyUpdateBehavior.Immediate,
        _ when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => PropertyUpdateBehavior.Immediate,
        _ => throw new ArgumentException("Expected a known OS platform. Behavior on current platform is unknown.")
    };

    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var directoryInfo = Directory.CreateDirectory(tempFolder);

        return Task.FromResult<IModifiableFolder>(new SystemFolder(directoryInfo.FullName));
    }

    public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
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

        // Filesystem mtime resolution is limited by kernel jiffy on Linux (~4ms) and by NTFS
        // update granularity on Windows. Without a delay, a write immediately after creation
        // can land in the same tick, producing an identical mtime and making timestamp-change
        // assertions unreliable under load.
        await Task.Delay(10);

        return new SystemFolder(tempFolder);
    }

    public override Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Task.FromResult<IFolder?>(null);

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

    public override Task<CreateFileInFolderWithTimestampsResult?> CreateFileInFolderWithTimestampsAsync(IModifiableFolder folder, DateTime? createdAt, DateTime? lastModifiedAt, DateTime? lastAccessedAt)
    {
        if (folder is not SystemFolder systemFolder)
            return Task.FromResult<CreateFileInFolderWithTimestampsResult?>(null);

        var filePath = Path.Combine(systemFolder.Path, $"{Guid.NewGuid()}.tmp");
        using (var fs = File.Create(filePath))
        {
            // Write some content
            var data = new byte[1024];
            new Random().NextBytes(data);
            fs.Write(data);
        }

        CreateFileInFolderWithTimestampsResult? returnTuple = new(new SystemFile(filePath));

        // Skip creation for linux, setting birthtime not yet implemented in kernel as of 2.24.2026.
        // Test base class will skip checks on createdAt since SystemFile/SystemFolder don't implement modifiability on Linux.
        if (createdAt.HasValue && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && returnTuple.CreatedAt is not null)
        {
            returnTuple.CreatedAt = createdAt.Value;
            File.SetCreationTimeUtc(filePath, createdAt.Value);
        }
        if (lastModifiedAt.HasValue && returnTuple is not null && returnTuple.LastModifiedAt is not null)
        {
            returnTuple.LastModifiedAt = lastModifiedAt.Value;
            File.SetLastWriteTimeUtc(filePath, lastModifiedAt.Value);
        }
        if (lastAccessedAt.HasValue && returnTuple is not null && returnTuple.LastAccessedAt is not null)
        {
            returnTuple.LastAccessedAt = lastAccessedAt.Value;
            File.SetLastAccessTimeUtc(filePath, lastAccessedAt.Value);
        }

        return Task.FromResult(returnTuple);
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
