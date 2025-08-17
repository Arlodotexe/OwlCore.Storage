using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Storage;

namespace OwlCore.Console.Storage;

/// <summary>
/// An <see cref="IChildFile"/> wrapper that gives a custom parent to any <see cref="IFile"/>.
/// </summary>
public class ParentOverrideChildFile : IChildFile
{
    /// <inheritdoc />
    public required IFile Inner { get; init; }

    /// <summary>
    /// The custom parent for this file.
    /// </summary>
    public required IFolder? Parent { get; init; }

    /// <inheritdoc />
    public string Id => Inner.Id;

    /// <inheritdoc />
    public string Name => Inner.Name;

    /// <inheritdoc />
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default) => Task.FromResult(Parent);

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default) => Inner.OpenStreamAsync(accessMode, cancellationToken);
}
