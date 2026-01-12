using System;
using System.IO;

namespace OwlCore.Storage.System.IO;

/// <summary>Last modified timestamp property for System.IO-backed storage items.</summary>
public sealed class SystemIOLastModifiedAtProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTime?>(
        id: owner.Id + "/" + nameof(ILastModifiedAt.LastModifiedAt),
        name: nameof(ILastModifiedAt.LastModifiedAt),
        getter: () => info.LastWriteTime,
        setter: v => info.LastWriteTime = v ?? throw new ArgumentNullException(nameof(v), "Cannot set last modified time to null.")
    ), IModifiableLastModifiedAtProperty
{ }
