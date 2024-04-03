using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for opening and reading the storage content of <see cref="IFile"/> instances.
/// </summary>
public static class FileReadExtensions
{
    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static Task<byte[]> ReadBytesAsync(this IFile file, CancellationToken cancellationToken = default) => file.ReadBytesAsync(bufferSize: 81920, cancellationToken);

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static async Task<byte[]> ReadBytesAsync(this IFile file, int bufferSize = 81920, CancellationToken cancellationToken = default)
    {
        using var stream = await file.OpenReadAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, bufferSize, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="encoding">The encoding to use for reading text from the file.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static async Task<string> ReadTextAsync(this IFile file, Encoding encoding, int bufferSize = 81920, CancellationToken cancellationToken = default)
    {
        var bytes = await file.ReadBytesAsync(bufferSize, cancellationToken);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="encoding">The encoding to use for reading text from the file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static Task<string> ReadTextAsync(this IFile file, Encoding encoding, CancellationToken cancellationToken = default) => file.ReadTextAsync(encoding, 81920, cancellationToken: cancellationToken);

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static Task<string> ReadTextAsync(this IFile file, CancellationToken cancellationToken = default) => file.ReadTextAsync(Encoding.UTF8, cancellationToken);
}