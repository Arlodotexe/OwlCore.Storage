using System;
using System.IO;

namespace OwlCore.Storage.System.IO;

/// <summary>Creation timestamp with offset property for System.IO-backed storage items.</summary>
public sealed class SystemIOCreatedAtOffsetProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTimeOffset?>(
        id: owner.Id + "/" + nameof(ICreatedAtOffset.CreatedAtOffset),
        name: nameof(ICreatedAtOffset.CreatedAtOffset),
        getter: () => new DateTimeOffset(info.CreationTimeUtc, TimeSpan.Zero),
        setter: v => info.CreationTimeUtc = v?.UtcDateTime ?? throw new ArgumentNullException(nameof(v), "Cannot set creation time to null.")
    ), IModifiableCreatedAtOffsetProperty
{ }
