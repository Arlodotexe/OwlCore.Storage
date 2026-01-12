using System;
using System.IO;

namespace OwlCore.Storage.System.IO;

/// <summary>Last accessed timestamp with offset property for System.IO-backed storage items.</summary>
public sealed class SystemIOLastAccessedAtOffsetProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTimeOffset?>(
        id: owner.Id + "/" + nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
        name: nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
        getter: () => new DateTimeOffset(info.LastAccessTimeUtc, TimeSpan.Zero),
        setter: v => info.LastAccessTimeUtc = v?.UtcDateTime ?? throw new ArgumentNullException(nameof(v), "Cannot set last accessed time to null.")
    ), IModifiableLastAccessedAtOffsetProperty
{ }
