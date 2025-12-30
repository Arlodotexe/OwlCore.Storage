using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Indicates that the storage item supports modifying the creation timestamp as a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// The parameter is non-nullable because most underlying storage systems do not support setting a null or "unset" timestamp value.
/// Property lifecycle is tied to the storage container; there is no "delete" semantic for individual properties.
/// </remarks>
public interface IModifiableCreatedAtOffset : ICreatedAtOffset, IModifiableCreatedAt
{
    /// <summary>
    /// Asynchronously updates the creation timestamp of the storage item with timezone offset information.
    /// </summary>
    /// <param name="createdDateTime">The new creation timestamp to set, including timezone offset.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateCreatedAtOffsetAsync(DateTimeOffset createdDateTime, CancellationToken cancellationToken);
}
