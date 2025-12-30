using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item supports modifying the last modified timestamp as a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// The parameter is non-nullable because most underlying storage systems do not support setting a null or "unset" timestamp value.
/// Property lifecycle is tied to the storage container; there is no "delete" semantic for individual properties.
/// </remarks>
public interface IModifiableLastModifiedAtOffset : ILastModifiedAtOffset, IModifiableLastModifiedAt
{
    /// <summary>
    /// Asynchronously updates the last modified timestamp of the storage item with timezone offset information.
    /// </summary>
    /// <param name="lastModifiedDateTime">The new last modified timestamp to set, including timezone offset.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateLastModifiedAtAsync(DateTimeOffset lastModifiedDateTime, CancellationToken cancellationToken);
}
