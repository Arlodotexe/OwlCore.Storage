using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="ILastAccessedAtOffsetProperty"/> to support watching for changes to the last accessed timestamp.
/// </summary>
public interface IMutableLastAccessedAtOffsetProperty : ILastAccessedAtOffsetProperty, IMutableStorageProperty<DateTimeOffset?>
{
}
