using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.Net.Http;

/// <summary>Last modified timestamp property for http-backed storage items.</summary>
public sealed class HttpLastModifiedAtProperty : SimpleStorageProperty<DateTime?>, ILastModifiedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="HttpLastModifiedAtProperty"/>.
    /// </summary>
    /// <param name="file">The storable that this property belongs to. Used to compute the Id.</param>
    public HttpLastModifiedAtProperty(HttpFile file) : base(
            id: file.Id + "/" + nameof(ILastModifiedAt.LastModifiedAt),
            name: nameof(ILastModifiedAt.LastModifiedAt),
            asyncGetter: async (c) => await GetLastModifiedAsync(file, c)
    )
    {
    }

    private static async Task<DateTime?> GetLastModifiedAsync(HttpFile file, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, file.Uri);
        var response = await file.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Extract the content length if available
        return response.Content.Headers.LastModified?.LocalDateTime;
    }
}
