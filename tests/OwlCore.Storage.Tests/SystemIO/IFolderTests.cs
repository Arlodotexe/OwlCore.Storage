using OwlCore.Storage.CommonTests;
using OwlCore.Storage.SystemIO;

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
        var dir = Directory.CreateDirectory(tempFolder);

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
}