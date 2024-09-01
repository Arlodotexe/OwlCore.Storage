using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;
using OwlCore.Storage.System.IO.Compression;
using System.IO.Compression;

namespace OwlCore.Storage.Tests.Archive.ZipArchive;

[TestClass]
public class ZipArchiveFolder_FileOnDisk_Tests : CommonIModifiableFolderTests
{
    // Required for base class to perform common tests.
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        var sourceFile = new SystemFile(CreateEmptyArchiveOnDisk());
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

    private static string CreateEmptyArchiveOnDisk()
    {
        // Create new archive on disk
        string archiveId = $"archiveTest_{Guid.NewGuid()}";
        var tempArchivePath = Path.Combine(Path.GetTempPath(), archiveId);

        var dir = Directory.CreateDirectory(tempArchivePath);
        ZipFile.CreateFromDirectory(tempArchivePath, tempArchivePath += ".zip");

        return tempArchivePath;
    }
}
