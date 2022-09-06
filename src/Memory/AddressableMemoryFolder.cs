using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>
/// An addressable folder implementation that resides in memory.
/// </summary>
public class AddressableMemoryFolder : MemoryFolder, IAddressableFolder
{
    /// <summary>
    /// A list of parent folders. The first item is the root, the last item is the parent.
    /// </summary>
    public IFolder[] ParentChain { get; }

    /// <summary>
    /// Creates a new instance of <see cref="MemoryFolder"/>.
    /// </summary>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    /// <param name="parentChain">A list of parent folders. The first item is the root, the last item is the parent.</param>
    public AddressableMemoryFolder(string name, IFolder[] parentChain)
        : base(CreatePath(name, parentChain), name)
    {
        ParentChain = parentChain;
        Path = CreatePath(name, parentChain);
    }

    internal static string CreatePath(string name, IFolder[] parentChain)
    {
        if (parentChain.Length == 1)
            return $"{parentChain[0]}/{name}";
        else
            return $"{parentChain[0]}/{string.Join("/", parentChain.Select(x => x.Name))}/{name}";
    }

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ParentChain.Length == 0 ? null : ParentChain[ParentChain.Length - 1]);
    }
}