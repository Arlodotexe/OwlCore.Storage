using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;
using OwlCore.Storage.System.IO.Compression;
using System.IO.Compression;

namespace OwlCore.Storage.Tests.Archive.ZipArchive;

[TestClass]
public class ZipArchiveEntryFile_FileOnDisk_Tests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override async Task<IFile> CreateFileAsync()
    {
        // Create new archive on disk
        string entryId = Guid.NewGuid().ToString();
        string archiveId = $"archiveTest_{Guid.NewGuid()}";
        var tempArchivePath = Path.Combine(Path.GetTempPath(), archiveId);

        var dir = Directory.CreateDirectory(tempArchivePath);
        await using (var entryStream = File.Create(Path.Combine(tempArchivePath, entryId)))
        {
            var randomData = GenerateRandomData(256_000);
            entryStream.Write(randomData);
        }

        var archiveFullPath = tempArchivePath + ".zip";
        ZipFile.CreateFromDirectory(tempArchivePath, archiveFullPath);

        var createdArchive = new SystemFile(archiveFullPath);
        var zipFolder = new ZipArchiveFolder(createdArchive);

        // Manually open the archive before using it directly.
        await zipFolder.OpenArchiveAsync();
        Assert.IsNotNull(zipFolder.Archive);

        var entry = zipFolder.Archive.GetEntry(entryId);
        Assert.IsNotNull(entry);

        var file = new ZipArchiveEntryFile(entry, zipFolder);

        return file;

        static byte[] GenerateRandomData(int length)
        {
            var rand = new Random();
            var b = new byte[length];
            rand.NextBytes(b);

            return b;
        }
    }

    // ZipArchiveEntryFile doesn't support setting timestamps at creation
    public override Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt) => Task.FromResult<IFile?>(null);
    public override Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt) => Task.FromResult<IFile?>(null);
    public override Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt) => Task.FromResult<IFile?>(null);
}
