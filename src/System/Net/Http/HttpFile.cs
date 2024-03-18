using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.Net.Http;

/// <summary>
/// A file implementation which calls GET on a provided HTTP URL and returns it for <see cref="OpenStreamAsync"/>.
/// </summary>
public class HttpFile : IFile
{
    /// <summary>
    /// Creates a new instance of <see cref="HttpFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    public HttpFile(Uri uri)
        : this(uri, new HttpClient(new HttpClientHandler()))
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="HttpFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    public HttpFile(string uri)
        : this(uri, new HttpClient(new HttpClientHandler()))
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="HttpFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    /// <param name="httpClient">The client to use for requests.</param>
    public HttpFile(Uri uri, HttpClient httpClient)
    {
        Uri = uri;
        Name = Path.GetFileName(uri.AbsolutePath);
        Id = uri.OriginalString;

        Client = httpClient;
    }

    /// <summary>
    /// Creates a new instance of <see cref="HttpFile"/>.
    /// </summary>
    /// <param name="uri">The http address to GET for the file content.</param>
    /// <param name="httpClient">The client to use for requests.</param>
    public HttpFile(string uri, HttpClient httpClient)
        : this(new Uri(uri), httpClient)
    {
    }

    /// <summary>
    /// Gets the message handler to use for making HTTP requests.
    /// </summary>
    public HttpClient Client { get; init; }

    /// <summary>
    /// Gets the http address to GET for the file content.
    /// </summary>
    public Uri Uri { get; }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (accessMode == 0)
            throw new ArgumentOutOfRangeException(nameof(accessMode), $"{nameof(FileAccess)}.{accessMode} is not valid here.");

        if (accessMode == FileAccess.Write)
            throw new NotSupportedException($"{nameof(FileAccess)}.{accessMode} is not supported over Http.");

        var request = new HttpRequestMessage(HttpMethod.Get, Uri);
        var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();

        // Extract the content length if available
        long? length = response.Content.Headers.ContentLength;

        if (length is long notNullLength)
            contentStream = new LengthOverrideStream(contentStream, notNullLength);

        // Return in a lazy seek-able wrapper.
        return new LazySeekStream(contentStream);
    }
}