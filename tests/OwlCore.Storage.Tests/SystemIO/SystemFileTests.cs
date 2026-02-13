using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests.SystemIO;

[TestClass]
public class SystemFileTests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override async Task<IFile> CreateFileAsync()
    {
        var filePath = await GenerateRandomFile(256_000);
        return new SystemFile(filePath);
    }

    public override async Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt)
    {
        var filePath = await GenerateRandomFile(256_000);
        File.SetCreationTimeUtc(filePath, createdAt);
        return new SystemFile(filePath);
    }

    public override async Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var filePath = await GenerateRandomFile(256_000);
        File.SetLastWriteTimeUtc(filePath, lastModifiedAt);
        return new SystemFile(filePath);
    }

    public override async Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt)
    {
        var filePath = await GenerateRandomFile(256_000);
        File.SetLastAccessTimeUtc(filePath, lastAccessedAt);
        return new SystemFile(filePath);
    }

    private static async Task<string> GenerateRandomFile(int fileSize)
    {
        // Create
        var tempFilePath = Path.GetTempFileName();
        await using var tempFileStr = File.Create(tempFilePath);

        // Write
        tempFileStr.Position = 0;
        await tempFileStr.WriteAsync(GenerateRandomData(fileSize), 0, fileSize);

        return tempFilePath;
    }

    private static byte[] GenerateRandomData(int length)
    {
        var rand = new Random();
        var b = new byte[length];
        rand.NextBytes(b);

        return b;
    }
}
