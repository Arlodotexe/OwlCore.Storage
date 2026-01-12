using System;
using System.IO;

namespace OwlCore.Storage.System.IO;

/// <summary>Creation timestamp property for System.IO-backed storage items.</summary>
public sealed class SystemIOCreatedAtProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTime?>(
        id: owner.Id + "/" + nameof(ICreatedAt.CreatedAt),
        name: nameof(ICreatedAt.CreatedAt),
        getter: () => info.CreationTime,
        setter: v => info.CreationTime = v ?? throw new ArgumentNullException(nameof(v), "Cannot set creation time to null.")
    ), IModifiableCreatedAtProperty
{ }
