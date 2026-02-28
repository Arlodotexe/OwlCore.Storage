using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;
using OwlCore.Storage.System.IO.Compression;
using System.IO.Compression;

namespace OwlCore.Storage.Tests.Archive.ZipArchive;

/// <summary>
/// A test suite for <see cref="ZipArchiveEntryFile"/>s created entirely in memory.
/// </summary>
[TestClass]
public class ZipArchiveEntryFile_FileInMemory_Tests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override Task<IFile> CreateFileAsync()
    {
        var archiveStream = new MemoryStream();
        var sourceFile = new MemoryFile($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", archiveStream);
        var archive = new global::System.IO.Compression.ZipArchive(archiveStream, ZipArchiveMode.Update);
        var entry = archive.CreateEntry($"{Guid.NewGuid()}");

        using (var entryStream = entry.Open())
        {
            var randomData = GenerateRandomData(256_000);
            entryStream.Write(randomData);
        }

        var file = new ZipArchiveEntryFile(entry, new ZipArchiveFolder(sourceFile));

        return Task.FromResult<IFile>(file);

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
