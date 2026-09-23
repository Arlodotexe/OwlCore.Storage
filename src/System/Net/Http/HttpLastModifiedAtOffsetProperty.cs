using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.Net.Http;

/// <summary>Last modified timestamp with offset property for http-backed storage items.</summary>
public sealed class HttpLastModifiedAtOffsetProperty : SimpleStorageProperty<DateTimeOffset?>, ILastModifiedAtOffsetProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="HttpLastModifiedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="file">The storable that this property belongs to. Used to compute the Id.</param>
    public HttpLastModifiedAtOffsetProperty(HttpFile file) : base(
            id: file.Id + "/" + nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            name: nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            asyncGetter: async (c) => await GetLastModifiedAsync(file, c)
    )
    {
    }


    private static async Task<DateTimeOffset?> GetLastModifiedAsync(HttpFile file, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, file.Uri);
        var response = await file.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Extract the content length if available
        return response.Content.Headers.LastModified;
    }
}
