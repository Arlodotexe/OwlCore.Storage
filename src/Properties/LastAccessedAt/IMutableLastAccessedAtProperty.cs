using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="ILastAccessedAtProperty"/> to support watching for changes to the last accessed timestamp.
/// </summary>
/// <remarks>
/// Call <see cref="IMutableStorageProperty{T}.GetWatcherAsync"/> to obtain a disposable watcher
/// that raises events when the last accessed timestamp changes.
/// </remarks>
public interface IMutableLastAccessedAtProperty : ILastAccessedAtProperty, IMutableStorageProperty<DateTime?>
{
}
