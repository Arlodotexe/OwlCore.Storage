using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item supports modifying the last accessed timestamp as a <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// The parameter is non-nullable because most underlying storage systems do not support setting a null or "unset" timestamp value.
/// Property lifecycle is tied to the storage container; there is no "delete" semantic for individual properties.
/// </remarks>
public interface IModifiableLastAccessedAt : ILastAccessedAt
{
    /// <summary>
    /// Asynchronously updates the last accessed timestamp of the storage item.
    /// </summary>
    /// <param name="lastAccessedDateTime">The new last accessed timestamp to set.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateLastAccessedAtAsync(DateTime lastAccessedDateTime, CancellationToken cancellationToken);
}
