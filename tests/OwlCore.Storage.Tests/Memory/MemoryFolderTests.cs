using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests.Memory;

[TestClass]
public class MemoryFolderTests : CommonIModifiableFolderTests
{
    // Required for base class to perform common tests.
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        return Task.FromResult<IModifiableFolder>(new MemoryFolder($"{Guid.NewGuid()}", $"{Guid.NewGuid()}"));
    }

    public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
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