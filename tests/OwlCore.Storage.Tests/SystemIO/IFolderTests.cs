using OwlCore.Storage.SystemIO;

namespace OwlCore.Storage.Tests.SystemIO;

[TestClass]
public class IFolderTests : CommonIFolderTests
{
    // Required for base class to perform common tests.
    public override Task<IFolder> CreateFolder()
    {
        var directoryInfo = Directory.CreateDirectory(Path.GetTempPath());

        return Task.FromResult<IFolder>(new SystemFolder(directoryInfo.FullName));
    }

    // Required for base class to perform common tests.
    public override Task<IFolder> CreateFolderUsingInvalidParameters()
    {
        return Task.FromResult<IFolder>(new SystemFolder(Guid.NewGuid().ToString()));
    }

    public override Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        for (var i = 0; i < fileCount; i++)
        {
            var path = Path.Combine(tempFolder, $"File.{i}.tmp");
            using var _ = File.Create(path);
        }

        for (var i = 0; i < folderCount; i++)
        {
            var path = Path.Combine(tempFolder, $"Folder.{i}");
            Directory.CreateDirectory(path);
        }

        return Task.FromResult<IFolder>(new SystemFolder(tempFolder));
    }
}