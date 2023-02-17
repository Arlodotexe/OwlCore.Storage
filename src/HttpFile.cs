using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A file implementation which calls GET on a provided HTTP URL and returns it for <see cref="OpenStreamAsync"/>.
/// </summary>
public class HttpFile : IFile
{
    private HttpClient? _client;

    /// <summary>
    /// Creates a new instance of <see cref="HttpFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    public HttpFile(Uri uri)
    {
        Uri = uri;
        Name = Path.GetFileName(uri.AbsolutePath);
        Id = uri.OriginalString;
        MessageHandler = new HttpClientHandler();
    }

    /// <summary>
    /// Creates a new instance of <see cref="HttpFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    public HttpFile(string uri)
        : this(new Uri(uri))
    {
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
    public string Name { get; init; }

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