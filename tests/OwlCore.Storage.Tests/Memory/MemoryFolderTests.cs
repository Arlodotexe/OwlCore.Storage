using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests.Memory;

[TestClass]
public class MemoryFolderTests : CommonIModifiableFolderTests
{
    public override PropertyValueAvailability CreatedAtAvailability => PropertyValueAvailability.Always;
    public override PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Maybe;
    public override PropertyValueAvailability LastModifiedAtAvailability => PropertyValueAvailability.Maybe;

    // Required for base class to perform common tests.
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        return Task.FromResult<IModifiableFolder>(new MemoryFolder());
    }

    public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
    {
        var folder = new MemoryFolder();

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

    public override async Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt)
    {
        var folder = new MemoryFolder();

        var created = (MemoryCreatedAtProperty)folder.CreatedAt;
        created.DateTimeValue.Value = createdAt;

        var createdOffset = (MemoryCreatedAtOffsetProperty)folder.CreatedAtOffset;
        createdOffset.DateTimeOffsetValue.Value = createdAt;

        return folder;
    }

    public override async Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var folder = new MemoryFolder();

        var lastModified = (MemoryLastModifiedAtProperty)folder.LastModifiedAt;
        lastModified.DateTimeValue.Value = lastModifiedAt;

        var lastModifiedOffset = (MemoryLastModifiedAtOffsetProperty)folder.LastModifiedAtOffset;
        lastModifiedOffset.DateTimeOffsetValue.Value = lastModifiedAt;

        return folder;
    }

    public override async Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt)
    {
        var folder = new MemoryFolder();

        var lastAccessed = (MemoryLastAccessedAtProperty)folder.LastAccessedAt;
        lastAccessed.DateTimeValue.Value = lastAccessedAt;

        var lastAccessedOffset = (MemoryLastAccessedAtOffsetProperty)folder.LastAccessedAtOffset;
        lastAccessedOffset.DateTimeOffsetValue.Value = lastAccessedAt;

        return folder;
    }

    public override async Task<IFile?> CreateFileInFolderWithLastModifiedAtAsync(IModifiableFolder folder, DateTime lastModifiedAt)
    {
        var file = (MemoryFile)await folder.CreateFileAsync(lastModifiedAt.ToString());

        var lastModified = (MemoryLastModifiedAtProperty)file.LastModifiedAt;
        lastModified.DateTimeValue.Value = lastModifiedAt;

        var lastModifiedOffset = (MemoryLastModifiedAtOffsetProperty)file.LastModifiedAtOffset;
        lastModifiedOffset.DateTimeOffsetValue.Value = lastModifiedAt;

        return file;
    }

    public override async Task<CreateFileInFolderWithTimestampsResult?> CreateFileInFolderWithTimestampsAsync(IModifiableFolder folder, DateTime? createdAt, DateTime? lastModifiedAt, DateTime? lastAccessedAt)
    {
        var file = (MemoryFile)await folder.CreateFileAsync(Guid.NewGuid().ToString());

        var lastModified = (MemoryLastModifiedAtProperty)file.LastModifiedAt;
        lastModified.DateTimeValue.Value = lastModifiedAt;

        var lastModifiedOffset = (MemoryLastModifiedAtOffsetProperty)file.LastModifiedAtOffset;
        lastModifiedOffset.DateTimeOffsetValue.Value = lastModifiedAt;

        var lastAccessed = (MemoryLastAccessedAtProperty)file.LastAccessedAt;
        lastAccessed.DateTimeValue.Value = lastAccessedAt;

        var lastAccessedOffset = (MemoryLastAccessedAtOffsetProperty)file.LastAccessedAtOffset;
        lastAccessedOffset.DateTimeOffsetValue.Value = lastAccessedAt;

        var created = (MemoryCreatedAtProperty)file.CreatedAt;
        created.DateTimeValue.Value = createdAt;

        var createdOffset = (MemoryCreatedAtOffsetProperty)file.CreatedAtOffset;
        createdOffset.DateTimeOffsetValue.Value = createdAt;

        return new(file, createdAt, lastModifiedAt, lastAccessedAt);
    }
}
