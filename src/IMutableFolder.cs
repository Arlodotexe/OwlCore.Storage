using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Represents a folder whose content can change.
/// </summary>
public interface IMutableFolder : IFolder
{
    /// <summary>
    /// Asynchronously retrieves a disposable object which can notify of changes to the folder.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation. The result is a disposable object which can notify of changes to the folder.</returns>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default);
}