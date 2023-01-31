using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Storage.Memory;

namespace OwlCore.Storage;

/// <summary>
/// A file implementation which calls GET on a provided HTTP URL and returns it for <see cref="OpenStreamAsync"/>.
/// </summary>
public class HttpFile : IFile
{
    private HttpClient? _client;

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    public HttpFile(Uri uri)
        : this(uri, uri.OriginalString, uri.OriginalString)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    public HttpFile(Uri uri, string id, string name)
    {
        Uri = uri;
        Id = id;
        Name = name;
        MessageHandler = new HttpClientHandler();
    }

    /// <summary>
    /// The default message handler to use for making HTTP requests.
    /// </summary>
    public HttpMessageHandler MessageHandler { get; }

    /// <summary>
    /// The http address to GET for the file content.
    /// </summary>
    public Uri Uri { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (accessMode == 0)
            throw new ArgumentOutOfRangeException(nameof(accessMode), $"{nameof(FileAccess)}.{accessMode} is not valid here.");

        if (accessMode == FileAccess.Write)
            throw new NotSupportedException($"{nameof(FileAccess)}.{accessMode} is not supported over Http.");

        _client ??= new HttpClient(MessageHandler);

        return _client.GetStreamAsync(Uri);
    }
}