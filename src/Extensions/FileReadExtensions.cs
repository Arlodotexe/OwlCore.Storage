using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static Task<byte[]> ReadBytesAsync(this IFile file) => file.ReadBytesAsync(default);

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
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static Task<string> ReadTextAsync(this IFile file) => file.ReadTextAsync(default);

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static Task<string> ReadTextAsync(this IFile file, CancellationToken cancellationToken = default) => file.ReadTextAsync(Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task containing a byte array of the file contents.</returns>
    public static async Task<string> ReadTextAsync(this IFile file, int bufferSize = 81920, CancellationToken cancellationToken = default)
    {
        var bytes = await file.ReadBytesAsync(bufferSize, cancellationToken);
        return Encoding.UTF8.GetString(bytes);
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
    /// Reads a specific range from the text file. 
    /// </summary>
    /// <param name="sourceFile">The file to read from.</param>
    /// <param name="lineRange">The line range to read.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A per-line async enumerable of the read content.</returns>
#if NETSTANDARD
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, (int Start, int End) lineRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#elif NET7_OR_GREATER
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, Range lineRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
    {
        using var fileStream = await sourceFile.OpenReadAsync(cancellationToken);
        using var streamReader = new StreamReader(fileStream);

        // Fast-forward to range start
        var currentLine = 0;
#if NETSTANDARD
        while (currentLine++ < lineRange.Start)
#elif NET7_OR_GREATER
        while (currentLine++ < lineRange.Start.Value)
#endif

        {
#if NETSTANDARD
            _ = await streamReader.ReadLineAsync();
#elif NET7_OR_GREATER
            _ = await streamReader.ReadLineAsync(cancellationToken);
#endif
        }

        // Read to range end
#if NETSTANDARD
        while (currentLine++ <= lineRange.End)
#elif NET7_OR_GREATER
        while (currentLine++ <= lineRange.End.Value)
#endif
        {
#if NETSTANDARD
            var line = await streamReader.ReadLineAsync();
#elif NET7_OR_GREATER
            var line = await streamReader.ReadLineAsync(cancellationToken);
#endif
            if (line is not null)
                yield return line;
        }
    }

    /// <summary>
    /// Reads a specific column range from each line within a line range in the text file.
    /// </summary>
    /// <param name="sourceFile">The file to read from.</param>
    /// <param name="lineRange">The line range to read.</param>
    /// <param name="columnRange">The character range within each line to return.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A per-line async enumerable of the read content.</returns>
#if NETSTANDARD
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, (int Start, int End) lineRange, (int Start, int End) columnRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#elif NET7_OR_GREATER
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, Range lineRange, Range columnRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
    {
        using var fileStream = await sourceFile.OpenReadAsync(cancellationToken);
        using var streamReader = new StreamReader(fileStream);

        // Skip to line range start
        var currentLine = 0;
#if NETSTANDARD
        while (currentLine++ < lineRange.Start)
            _ = await streamReader.ReadLineAsync();
#elif NET7_OR_GREATER
        while (currentLine++ < lineRange.Start.Value)
            _ = await streamReader.ReadLineAsync(cancellationToken);
#endif

        // Take to line range end
#if NETSTANDARD
    while (currentLine++ <= lineRange.End)
        {
            var line = await streamReader.ReadLineAsync();
            if (line is not null)
            {
                var start = columnRange.Start;
                var end = columnRange.End;
                var length = Math.Max(0, end - start);

                if (start >= line.Length)
                {
                    yield return string.Empty;
                }
                else
                {
                    var safeLen = Math.Min(length, line.Length - start);
                    yield return line.Substring(start, safeLen);
                }
            }
        }
#elif NET7_OR_GREATER
        while (currentLine++ <= lineRange.End.Value)
        {
            var line = await streamReader.ReadLineAsync(cancellationToken);
            if (line is not null)
                yield return new([.. line.Skip(columnRange.Start.Value).Take(columnRange.End.Value - columnRange.Start.Value)]);
        }
#endif
    }
}