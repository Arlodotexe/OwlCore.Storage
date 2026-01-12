using System;

namespace OwlCore.Storage;

/// <summary>
/// A disposable object which can notify of changes to a storage property value.
/// </summary>
/// <remarks>
/// <para>
/// This interface parallels <see cref="IFolderWatcher"/> but for single-valued property changes rather than
/// collection changes. The watcher owns any underlying resources needed to observe changes (e.g., <c>FileSystemWatcher</c>)
/// and disposing it releases those resources.
/// </para>
/// <para>
/// Separating the watcher from the property object keeps <see cref="IStorageProperty{T}"/> lightweight and
/// avoids forcing <see cref="IDisposable"/> onto all property implementations.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of property value being watched.</typeparam>
public interface IStoragePropertyWatcher<T> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the storage property being watched for changes.
    /// </summary>
    IStorageProperty<T> Property { get; }

    /// <summary>
    /// Raised when the property value is updated.
    /// </summary>
    event EventHandler<T>? ValueUpdated;
}