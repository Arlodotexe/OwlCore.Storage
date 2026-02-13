using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO;

/// <summary>Last modified timestamp with offset property for System.IO-backed storage items.</summary>
public sealed class SystemIOLastModifiedAtOffsetProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTimeOffset?>(
        id: owner.Id + "/" + nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
        name: nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
        getter: () => { info.Refresh(); return new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero); },
        setter: v => info.LastWriteTimeUtc = v?.UtcDateTime ?? throw new ArgumentNullException(nameof(v), "Cannot set last modified time to null.")
    ), IModifiableLastModifiedAtOffsetProperty
{
    /// <inheritdoc/>
    public override Task<IStoragePropertyWatcher<DateTimeOffset?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IStoragePropertyWatcher<DateTimeOffset?>>(new SystemIOPropertyWatcher<DateTimeOffset?>(this, info.FullName, NotifyFilters.LastWrite));
    }
}