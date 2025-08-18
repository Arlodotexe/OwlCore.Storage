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

    /// <summary>
    /// Writes only the specified line range from the provided <paramref name="content"/> into <paramref name="file"/> as UTF-8 text.
    /// </summary>
    /// <param name="file">The destination file to write to.</param>
    /// <param name="content">The full text content to slice and write.</param>
    /// <param name="lineRange">The line range (inclusive) from the content to write.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
#if NETSTANDARD
    public static async Task WriteTextAsync(this IFile file, string content, (int Start, int End) lineRange, CancellationToken cancellationToken = default)
#elif NET7_OR_GREATER
    public static async Task WriteTextAsync(this IFile file, string content, Range lineRange, CancellationToken cancellationToken = default)
#endif
    {
        using var writeStream = await file.OpenWriteAsync(cancellationToken);
        using var writer = new StreamWriter(writeStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: false);

        int startLine;
        int endLineInclusive;
#if NETSTANDARD
        startLine = lineRange.Start;
        endLineInclusive = lineRange.End;
#elif NET7_OR_GREATER
        startLine = lineRange.Start.Value;
        endLineInclusive = lineRange.End.Value;
#endif

        foreach (var line in SliceLines(content, startLine, endLineInclusive, null, null))
        {
            await writer.WriteLineAsync(line);
        }
#if NET7_OR_GREATER
        await writer.FlushAsync(cancellationToken);
#else
        await writer.FlushAsync();
#endif
    }

    /// <summary>
    /// Writes only the specified column range from each line within a line range from the provided <paramref name="content"/> into <paramref name="file"/> as UTF-8 text.
    /// </summary>
    /// <param name="file">The destination file to write to.</param>
    /// <param name="content">The full text content to slice and write.</param>
    /// <param name="lineRange">The line range (inclusive) from the content to write.</param>
    /// <param name="columnRange">The character range within each line to write (end exclusive).</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A Task representing the asynchronous write operation.</returns>
#if NETSTANDARD
    public static async Task WriteTextAsync(this IFile file, string content, (int Start, int End) lineRange, (int Start, int End) columnRange, CancellationToken cancellationToken = default)
#elif NET7_OR_GREATER
    public static async Task WriteTextAsync(this IFile file, string content, Range lineRange, Range columnRange, CancellationToken cancellationToken = default)
#endif
    {
        using var writeStream = await file.OpenWriteAsync(cancellationToken);
        using var writer = new StreamWriter(writeStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: false);

        int startLine;
        int endLineInclusive;
        int startCol;
        int endColExclusive;
#if NETSTANDARD
        startLine = lineRange.Start;
        endLineInclusive = lineRange.End;
        startCol = columnRange.Start;
        endColExclusive = columnRange.End;
#elif NET7_OR_GREATER
        startLine = lineRange.Start.Value;
        endLineInclusive = lineRange.End.Value;
        startCol = columnRange.Start.Value;
        endColExclusive = columnRange.End.Value;
#endif

        foreach (var line in SliceLines(content, startLine, endLineInclusive, startCol, endColExclusive))
        {
            await writer.WriteLineAsync(line);
        }
#if NET7_OR_GREATER
        await writer.FlushAsync(cancellationToken);
#else
        await writer.FlushAsync();
#endif
    }

    // Shared line/column slicer for both overloads to keep logic consistent across TFMs.
    private static global::System.Collections.Generic.IEnumerable<string> SliceLines(string content, int startLine, int endLineInclusive, int? startColumnOrNull, int? endColumnExclusiveOrNull)
    {
        using var reader = new StringReader(content);
        var current = 0;
        string? line;

        // Fast-forward to the starting line
        while (current++ < startLine && (line = reader.ReadLine()) is not null) { }

        // Emit each selected line (inclusive end)
        while (current++ <= endLineInclusive && (line = reader.ReadLine()) is not null)
        {
            if (startColumnOrNull is null || endColumnExclusiveOrNull is null)
            {
                yield return line;
                continue;
            }

            var startCol = startColumnOrNull.Value;
            var endColExclusive = endColumnExclusiveOrNull.Value; // exclusive end

            if (line.Length <= startCol)
            {
                yield return string.Empty;
                continue;
            }

            var length = endColExclusive - startCol;
            var safeLen = global::System.Math.Min(length, line.Length - startCol);
            yield return line.Substring(startCol, safeLen);
        }
    }
}
