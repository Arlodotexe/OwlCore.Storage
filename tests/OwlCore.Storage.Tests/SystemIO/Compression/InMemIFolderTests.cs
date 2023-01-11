using OwlCore.Storage.CommonTests;
using OwlCore.Storage.SystemIO.Compression;
using System.IO.Compression;

namespace OwlCore.Storage.Tests.SystemIO.Compression;

/// <summary>
/// A test suite for <see cref="ZipArchiveFolder"/>s created entirely in memory.
/// </summary>
[TestClass]
public class InMemIFolderTests : IModifiableFolderTests
{
    // Required for base class to perform common tests.
    public override Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        MemoryStream archiveStream = new();
        ZipArchive archive = new(archiveStream, ZipArchiveMode.Update);

        return Task.FromResult<IModifiableFolder>(new ZipArchiveFolder($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", archive));
    }

    public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
    {
        MemoryStream archiveStream = new();
        ZipArchive archive = new(archiveStream, ZipArchiveMode.Update);

        var folder = new ZipArchiveFolder($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", archive);

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