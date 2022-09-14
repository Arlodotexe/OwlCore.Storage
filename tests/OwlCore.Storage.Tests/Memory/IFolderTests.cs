using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests.Memory;

[TestClass]
public class IFolderTests : CommonIFolderTests
{
    // Required for base class to perform common tests.
    public override Task<IFolder> CreateFolderAsync()
    {
        return Task.FromResult<IFolder>(new MemoryFolder($"{Guid.NewGuid()}", $"{Guid.NewGuid()}"));
    }

    public override async Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount)
    {
        var folder = new MemoryFolder($"{Guid.NewGuid()}", $"{Guid.NewGuid()}");

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
}