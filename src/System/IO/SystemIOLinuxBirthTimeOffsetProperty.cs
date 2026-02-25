using System;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// A readonly creation offset timestamp property for System.IO-backed storage items on platforms
/// where birth time is not settable (e.g., Linux).
/// </summary>
internal sealed class SystemIOLinuxBirthTimeOffsetProperty(IStorable owner, string path)
    : SimpleStorageProperty<DateTimeOffset?>(owner.Id + "/" + nameof(ICreatedAtOffset.CreatedAtOffset), nameof(ICreatedAtOffset.CreatedAtOffset), () => StatxInterop.GetBirthTimeOffset(path)), ICreatedAtOffsetProperty;
