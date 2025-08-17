using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Storage;

namespace OwlCore.Console.Storage;

/// <summary>
/// Enables building a folder from an arbitrary list of files.
/// </summary>
public class ReadOnlyCompositeFolder : IFolder
{
    /// <inheritdoc />
    public required string Id { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }
    /// <inheritdoc />
    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Ensure asynchronous execution to avoid CS1998 and respect cancellation
        await Task.Yield();

        foreach (var file in Sources.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new ParentOverrideChildFile
            {
                Inner = file,
                Parent = this,
            };
        }
    }

    /// <inheritdoc />
    public ICollection<IFile> Sources { get; init; } = [];
}