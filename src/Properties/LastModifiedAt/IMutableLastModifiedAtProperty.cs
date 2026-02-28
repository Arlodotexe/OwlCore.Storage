using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="ILastModifiedAtProperty"/> to support watching for changes to the last modified timestamp.
/// </summary>
/// <remarks>
/// Call <see cref="IMutableStorageProperty{T}.GetWatcherAsync"/> to obtain a disposable watcher
/// that raises events when the last modified timestamp changes.
/// </remarks>
public interface IMutableLastModifiedAtProperty : ILastModifiedAtProperty, IMutableStorageProperty<DateTime?>
{
}
