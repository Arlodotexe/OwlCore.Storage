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
    public static Task<byte[]> ReadBytesAsync(this IFile file) => file.ReadBytesAsync(default);

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    public static Task<byte[]> ReadBytesAsync(this IFile file, CancellationToken cancellationToken = default) => file.ReadBytesAsync(bufferSize: 81920, cancellationToken);

    /// <summary>
    /// Opens the file for reading and reads the content into a byte array.
    /// </summary>
    public static async Task<byte[]> ReadBytesAsync(this IFile file, int bufferSize = 81920, CancellationToken cancellationToken = default)
    {
        using var stream = await file.OpenReadAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, bufferSize, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Opens the file for reading and reads the content into a string using UTF-8 encoding.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <returns>A task containing the string content of the file.</returns>
    public static Task<string> ReadTextAsync(this IFile file) => file.ReadTextAsync(default);

    /// <summary>
    /// Opens the file for reading and reads the content into a string using UTF-8 encoding.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the string content of the file.</returns>
    public static Task<string> ReadTextAsync(this IFile file, CancellationToken cancellationToken = default) => file.ReadTextAsync(Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Opens the file for reading and reads the content into a string using UTF-8 encoding.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer used when reading. The default size is 81920.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the string content of the file.</returns>
    public static async Task<string> ReadTextAsync(this IFile file, int bufferSize = 81920, CancellationToken cancellationToken = default)
    {
        var bytes = await file.ReadBytesAsync(bufferSize, cancellationToken);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Opens the file for reading and reads the content into a string using the specified encoding.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="encoding">The encoding to use for reading text from the file.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the string content of the file.</returns>
    public static Task<string> ReadTextAsync(this IFile file, Encoding encoding, CancellationToken cancellationToken = default) => file.ReadTextAsync(encoding, 81920, cancellationToken: cancellationToken);

    /// <summary>
    /// Opens the file for reading and reads the content into a string using the specified encoding.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <param name="encoding">The encoding to use for reading text from the file.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer used when reading. The default size is 81920.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the string content of the file.</returns>
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
    /// <summary>
    /// Reads a specific line range from the text file.
    /// </summary>
    /// <remarks>
    /// - Lines are 0-based indexed.
    /// - <c>End</c> is exclusive; the sequence yields lines in [<c>Start</c>, <c>End</c>).
    /// - If EOF is reached before <c>Start</c> or during enumeration, the sequence ends early without error.
    /// </remarks>
#if NETSTANDARD
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, (int Start, int End) lineRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#elif NET7_OR_GREATER
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, Range lineRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
    {
        using var fileStream = await sourceFile.OpenReadAsync(cancellationToken);
        using var streamReader = new StreamReader(fileStream);

#if NETSTANDARD
        var start = lineRange.Start;
        var end = lineRange.End;

        if (start < 0 || end < 0)
            throw new ArgumentOutOfRangeException(nameof(lineRange), "Start and End must be non-negative.");
        if (end < start)
            throw new ArgumentException("End must be greater than or equal to Start.", nameof(lineRange));
#elif NET7_OR_GREATER
        if (lineRange.Start.IsFromEnd || lineRange.End.IsFromEnd)
            throw new ArgumentException("From-end indices (^) are not supported.", nameof(lineRange));

        var start = lineRange.Start.Value;
        var end = lineRange.End.Value;

        if (start < 0 || end < 0)
            throw new ArgumentOutOfRangeException(nameof(lineRange), "Start and End must be non-negative.");
        if (end < start)
            throw new ArgumentException("End must be greater than or equal to Start.", nameof(lineRange));
#endif

        for (var i = 0; i < start; i++)
        {
#if NETSTANDARD
            cancellationToken.ThrowIfCancellationRequested();
            if (await streamReader.ReadLineAsync() is null)
                yield break;
#elif NET7_OR_GREATER
            if (await streamReader.ReadLineAsync(cancellationToken) is null)
                yield break;
#endif
        }

        for (var i = start; i < end; i++)
        {
#if NETSTANDARD
            cancellationToken.ThrowIfCancellationRequested();
            var line = await streamReader.ReadLineAsync();
#elif NET7_OR_GREATER
            var line = await streamReader.ReadLineAsync(cancellationToken);
#endif
            if (line is null)
                yield break;

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
    /// <summary>
    /// Reads a specific column range from each line within a line range.
    /// </summary>
    /// <remarks>
    /// - Lines and columns are 0-based indexed.
    /// - Both <c>lineRange.End</c> and <c>columnRange.End</c> are exclusive; lines and columns are taken from [Start, End).
    /// - If EOF is reached before <c>lineRange.Start</c> or during enumeration, the sequence ends early without error.
    /// - If <c>columnRange.Start</c> is beyond the end of a line, an empty string is yielded for that line.
    /// </remarks>
#if NETSTANDARD
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, (int Start, int End) lineRange, (int Start, int End) columnRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#elif NET7_OR_GREATER
    public static async IAsyncEnumerable<string> ReadTextAsync(this IFile sourceFile, Range lineRange, Range columnRange, [EnumeratorCancellation] CancellationToken cancellationToken)
#endif
    {
        using var fileStream = await sourceFile.OpenReadAsync(cancellationToken);
        using var streamReader = new StreamReader(fileStream);

#if NETSTANDARD
        var start = lineRange.Start;
        var end = lineRange.End;
        var colStart = columnRange.Start;
        var colEnd = columnRange.End;

        if (start < 0 || end < 0)
            throw new ArgumentOutOfRangeException(nameof(lineRange), "Start and End must be non-negative.");
        if (end < start)
            throw new ArgumentException("End must be greater than or equal to Start.", nameof(lineRange));

        if (colStart < 0 || colEnd < 0)
            throw new ArgumentOutOfRangeException(nameof(columnRange), "Start and End must be non-negative.");
        if (colEnd < colStart)
            throw new ArgumentException("End must be greater than or equal to Start.", nameof(columnRange));
#elif NET7_OR_GREATER
        if (lineRange.Start.IsFromEnd || lineRange.End.IsFromEnd)
            throw new ArgumentException("From-end indices (^) are not supported.", nameof(lineRange));
        if (columnRange.Start.IsFromEnd || columnRange.End.IsFromEnd)
            throw new ArgumentException("From-end indices (^) are not supported.", nameof(columnRange));

        var start = lineRange.Start.Value;
        var end = lineRange.End.Value;
        var colStart = columnRange.Start.Value;
        var colEnd = columnRange.End.Value;

        if (start < 0 || end < 0)
            throw new ArgumentOutOfRangeException(nameof(lineRange), "Start and End must be non-negative.");
        if (end < start)
            throw new ArgumentException("End must be greater than or equal to Start.", nameof(lineRange));

        if (colStart < 0 || colEnd < 0)
            throw new ArgumentOutOfRangeException(nameof(columnRange), "Start and End must be non-negative.");
        if (colEnd < colStart)
            throw new ArgumentException("End must be greater than or equal to Start.", nameof(columnRange));
#endif

        for (var i = 0; i < start; i++)
        {
#if NETSTANDARD
            cancellationToken.ThrowIfCancellationRequested();
            if (await streamReader.ReadLineAsync() is null)
                yield break;
#elif NET7_OR_GREATER
            if (await streamReader.ReadLineAsync(cancellationToken) is null)
                yield break;
#endif
        }

        for (var i = start; i < end; i++)
        {
#if NETSTANDARD
            cancellationToken.ThrowIfCancellationRequested();
            var line = await streamReader.ReadLineAsync();
#elif NET7_OR_GREATER
            var line = await streamReader.ReadLineAsync(cancellationToken);
#endif
            if (line is null)
                yield break;

            var length = Math.Max(0, colEnd - colStart);
            if (colStart >= line.Length)
            {
                yield return string.Empty;
            }
            else
            {
                var safeLen = Math.Min(length, line.Length - colStart);
                yield return line.Substring(colStart, safeLen);
            }
        }
    }
}