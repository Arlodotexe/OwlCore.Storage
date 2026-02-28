using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO;

/// <summary>Last accessed timestamp property for System.IO-backed storage items.</summary>
public sealed class SystemIOLastAccessedAtProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTime?>(
        id: owner.Id + "/" + nameof(ILastAccessedAt.LastAccessedAt),
        name: nameof(ILastAccessedAt.LastAccessedAt),
        getter: () => { info.Refresh(); return info.LastAccessTime; },
        setter: v => info.LastAccessTime = v ?? throw new ArgumentNullException(nameof(v), "Cannot set last accessed time to null.")
    ), IModifiableLastAccessedAtProperty
{
    /// <inheritdoc/>
    public override Task<IStoragePropertyWatcher<DateTime?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IStoragePropertyWatcher<DateTime?>>(new SystemIOPropertyWatcher<DateTime?>(this, info.FullName, NotifyFilters.LastAccess));
    }
}