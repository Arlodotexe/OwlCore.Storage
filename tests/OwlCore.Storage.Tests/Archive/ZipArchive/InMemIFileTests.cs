using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Archive;
using System.IO.Compression;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage.Tests.Archive.ZipArchive;

/// <summary>
/// A test suite for <see cref="ZipArchiveEntryFile"/>s created entirely in memory.
/// </summary>
[TestClass]
public class InMemIFileTests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override Task<IFile> CreateFileAsync()
    {
        var archiveStream = new MemoryStream();
        var sourceFile = new MemoryFile($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", archiveStream);
        var archive = new System.IO.Compression.ZipArchive(archiveStream, ZipArchiveMode.Update);
        var entry = archive.CreateEntry($"{Guid.NewGuid()}");

        using (var entryStream = entry.Open())
        {
            var randomData = GenerateRandomData(256_000);
            entryStream.Write(randomData);
        }

        var file = new ZipArchiveEntryFile(entry, new ZipArchiveFolder(archive, sourceFile));

        return Task.FromResult<IFile>(file);

        static byte[] GenerateRandomData(int length)
        {
            var rand = new Random();
            var b = new byte[length];
            rand.NextBytes(b);

            return b;
        }
    }
}
