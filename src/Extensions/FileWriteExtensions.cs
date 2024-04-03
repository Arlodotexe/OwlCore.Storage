using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for opening and writing to the storage content of <see cref="IFile"/> instances.
/// </summary>
public static class FileWriteExtensions
{
    /// <summary>
    /// Opens the file for writing and writes the byte array content to the file.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="content">The byte array content to write into the file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public static async Task WriteBytesAsync(this IFile file, byte[] content, CancellationToken cancellationToken = default)
    {
        using var stream = await file.OpenWriteAsync(cancellationToken);
        await stream.WriteAsync(content, 0, content.Length, cancellationToken);
    }

    /// <summary>
    /// Opens the file for writing and writes the string content to the file using UTF-8 encoding.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="content">The string content to write into the file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public static Task WriteTextAsync(this IFile file, string content, CancellationToken cancellationToken = default) => file.WriteTextAsync(content, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Opens the file for writing and writes the string content to the file using the specified encoding.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="content">The string content to write into the file.</param>
    /// <param name="encoding">The encoding to use for writing text to the file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
    public static async Task WriteTextAsync(this IFile file, string content, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var bytes = encoding.GetBytes(content);
        await file.WriteBytesAsync(bytes, cancellationToken);
    }
}
