using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="ILastModifiedAtOffsetProperty"/> to support watching for changes to the last modified timestamp.
/// </summary>
public interface IMutableLastModifiedAtOffsetProperty : ILastModifiedAtOffsetProperty, IMutableStorageProperty<DateTimeOffset?>
{
}
