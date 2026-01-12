using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="ICreatedAtOffsetProperty"/> to support watching for changes to the creation timestamp.
/// </summary>
public interface IMutableCreatedAtOffsetProperty : ICreatedAtOffsetProperty, IMutableStorageProperty<DateTimeOffset?>
{
}
