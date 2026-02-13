using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO;

/// <summary>Creation timestamp property for System.IO-backed storage items.</summary>
public sealed class SystemIOCreatedAtProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTime?>(
        id: owner.Id + "/" + nameof(ICreatedAt.CreatedAt),
        name: nameof(ICreatedAt.CreatedAt),
        getter: () => { info.Refresh(); return info.CreationTime; },
        setter: v => info.CreationTime = v ?? throw new ArgumentNullException(nameof(v), "Cannot set creation time to null.")
    ), IModifiableCreatedAtProperty
{
    /// <inheritdoc/>
    public override Task<IStoragePropertyWatcher<DateTime?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IStoragePropertyWatcher<DateTime?>>(new SystemIOPropertyWatcher<DateTime?>(this, info.FullName, NotifyFilters.CreationTime));
    }
}