using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for <see cref="IFile"/>.
/// </summary>
public static partial class FileExtensions
{
    /// <summary>
    /// Opens the file for reading.
    /// </summary>
    /// <param name="file">The file to open.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the requested stream.</returns>
    public static Task<Stream> OpenReadAsync(this IFile file, CancellationToken cancellationToken = default) => file.OpenStreamAsync(FileAccess.Read, cancellationToken);
    
    /// <summary>
    /// Opens the file for writing.
    /// </summary>
    /// <param name="file">The file to open.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the requested stream.</returns>
    public static Task<Stream> OpenWriteAsync(this IFile file, CancellationToken cancellationToken = default) => file.OpenStreamAsync(FileAccess.Write, cancellationToken);
    
    /// <summary>
    /// Opens the file for reading and writing.
    /// </summary>
    /// <param name="file">The file to open.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task containing the requested stream.</returns>
    public static Task<Stream> OpenReadWriteAsync(this IFile file, CancellationToken cancellationToken = default) => file.OpenStreamAsync(FileAccess.ReadWrite, cancellationToken);
}