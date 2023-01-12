using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Archive;
using System.IO.Compression;

namespace OwlCore.Storage.Tests.Archive.Zip;

/// <summary>
/// A test suite for <see cref="ZipEntryFile"/>s created entirely in memory.
/// </summary>
[TestClass]
public class InMemIFileTests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override Task<IFile> CreateFileAsync()
    {
        MemoryStream archiveStream = new();
        ZipArchive archive = new(archiveStream, ZipArchiveMode.Update);
        var entry = archive.CreateEntry($"{Guid.NewGuid()}");

        using (var entryStream = entry.Open())
        {
            var randomData = GenerateRandomData(256_000);
            entryStream.Write(randomData);
        }

        var file = new ZipEntryFile(entry, new ZipFolder($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", archive));

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
