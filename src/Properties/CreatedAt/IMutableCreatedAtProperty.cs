using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="ICreatedAtProperty"/> to support watching for changes to the creation timestamp.
/// </summary>
/// <remarks>
/// Call <see cref="IMutableStorageProperty{T}.GetWatcherAsync"/> to obtain a disposable watcher
/// that raises events when the creation timestamp changes.
/// </remarks>
public interface IMutableCreatedAtProperty : ICreatedAtProperty, IMutableStorageProperty<DateTime?>
{
}
