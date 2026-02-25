using System;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// A readonly creation timestamp property for System.IO-backed storage items on platforms
/// where birth time is not settable (e.g., Linux).
/// </summary>
internal sealed class SystemIOLinuxBirthTimeProperty(IStorable owner, string path)
    : SimpleStorageProperty<DateTime?>(owner.Id + "/" + nameof(ICreatedAt.CreatedAt), nameof(ICreatedAt.CreatedAt), () => StatxInterop.GetBirthTime(path)), ICreatedAtProperty;
