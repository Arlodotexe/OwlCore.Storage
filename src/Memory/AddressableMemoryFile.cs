using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;


/// <summary>
/// An addressable file implementation that resides in memory.
/// </summary>
public class AddressableMemoryFile : MemoryFile, IAddressableFile
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryFile"/>.
    /// </summary>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    /// <param name="memoryStream">An existing stream which is provided as the file contents.</param>
    /// <param name="parentChain">A list of parent folders. The first item is the root, the last item is the parent.</param>
    public AddressableMemoryFile(string name, MemoryStream memoryStream, IFolder[] parentChain)
        : base(CreatePath(name, parentChain), name, memoryStream)
    {
        Path = CreatePath(name, parentChain);
        ParentChain = parentChain;
    }

    /// <summary>
    /// A list of parent folders. The first item is the root, the last item is the parent.
    /// </summary>
    public IFolder[] ParentChain { get; set; }

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ParentChain.Length == 0 ? null : ParentChain[ParentChain.Length - 1]);
    }

    internal static string CreatePath(string name, IFolder[] parentChain)
    {
        if (parentChain.Length == 1)
            return $"{parentChain[0]}/{name}";
        else
            return $"{parentChain[0]}/{string.Join("/", parentChain.Select(x => x.Name))}/{name}";
    }
}

