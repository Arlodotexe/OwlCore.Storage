using System;
using System.IO;

namespace OwlCore.Storage.System.IO;

/// <summary>Last modified timestamp with offset property for System.IO-backed storage items.</summary>
public sealed class SystemIOLastModifiedAtOffsetProperty(IStorable owner, FileSystemInfo info)
    : SimpleModifiableStorageProperty<DateTimeOffset?>(
        id: owner.Id + "/" + nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
        name: nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
        getter: () => new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
        setter: v => info.LastWriteTimeUtc = v?.UtcDateTime ?? throw new ArgumentNullException(nameof(v), "Cannot set last modified time to null.")
    ), IModifiableLastModifiedAtOffsetProperty
{ }
