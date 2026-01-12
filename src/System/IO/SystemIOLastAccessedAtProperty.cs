using System;
using System.IO;

namespace OwlCore.Storage.System.IO;

/// <summary>Last accessed timestamp property for System.IO-backed storage items.</summary>
public sealed class SystemIOLastAccessedAtProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTime?>(
        id: owner.Id + "/" + nameof(ILastAccessedAt.LastAccessedAt),
        name: nameof(ILastAccessedAt.LastAccessedAt),
        getter: () => info.LastAccessTime,
        setter: v => info.LastAccessTime = v ?? throw new ArgumentNullException(nameof(v), "Cannot set last accessed time to null.")
    ), IModifiableLastAccessedAtProperty
{ }
