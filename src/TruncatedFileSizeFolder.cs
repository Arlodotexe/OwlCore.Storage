using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using OwlCore.Storage;

namespace OwlCore.Storage;

/// <summary>
/// A folder that limits each file to a given byte length. 
/// </summary>
/// <param name="Folder"></param>
/// <param name="MaxFileLength"></param>
public class TruncatedFilesFolder(IFolder Folder, int MaxFileLength) : IFolder
{
    /// <summary>
    /// The wrapped folder.
    /// </summary>
    public IFolder Folder { get; } = Folder;

    /// <summary>
    /// The max byte length for the files in this folder.
    /// </summary>
    public int MaxFileLength { get; } = MaxFileLength;

    /// <inheritdoc/>
    public string Id => Folder.Id;

    /// <inheritdoc/>
    public string Name => Folder.Name;

    /// <inheritdoc/>
    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in Folder.GetItemsAsync(type, cancellationToken))
        {
            if (item is IChildFile file)
            {
                yield return new ParentOverrideChildFile()
                {
                    Inner = new TruncatedFile(file, MaxFileLength),
                    Parent = await file.GetParentAsync(cancellationToken),
                };
            }
            else
            {
                yield return item;
            }
        }
    }
}