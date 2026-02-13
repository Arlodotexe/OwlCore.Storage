using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.System.IO;

/// <summary>
/// A Data watcher that wraps a <see cref="FileSystemWatcher"/> and raises events when a specific property changes.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class SystemIOPropertyWatcher<T> : IStoragePropertyWatcher<T>
{
    private readonly FileSystemWatcher _watcher;

    /// <summary>
    /// Creates a new instance of <see cref="SystemIOPropertyWatcher{T}"/>.
    /// </summary>
    /// <param name="property">The property being watched.</param>
    /// <param name="path">The path to the file or folder being watched.</param>
    /// <param name="filters">The notify filters to watch for.</param>
    public SystemIOPropertyWatcher(IStorageProperty<T> property, string path, NotifyFilters filters)
    {
        Property = property;

        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);

        // Handle root directories or cases where GetDirectoryName returns null
        if (string.IsNullOrEmpty(directory))
        {
            // Fallback for when we can't determine a parent (e.g. drive root like C:\)
            // System.IO watcher requires a directory.
            // If the path itself is a directory, using it as the watcher path means we watch its *children*, not the directory itself.
            // So technically we cannot watch properties of C:\ using FileSystemWatcher easily.
            // But we must support valid scenarios.
            if (Directory.Exists(path))
            {
                // This watches for changes INSIDE the folder, not the folder itself.
                // However, commonly LastWriteTime of a folder updates when children change.
                _watcher = new FileSystemWatcher(path);
            }
            else
            {
                 throw new ArgumentException($"Cannot determine parent directory for path '{path}' to watch for property changes.");   
            }
        }
        else
        {
            _watcher = new FileSystemWatcher(directory, fileName);
        }

        _watcher.NotifyFilter = filters;
        _watcher.Changed += OnChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Don't block the event handler
        Task.Run(async () =>
        {
            try
            {
                var value = await Property.GetValueAsync(CancellationToken.None);
                ValueUpdated?.Invoke(this, value);
            }
            catch
            {
                // In case of error (e.g. file lock, race condition), we suppress the update.
            }
        });
    }

    /// <inheritdoc/>
    public IStorageProperty<T> Property { get; }

    /// <inheritdoc/>
    public event EventHandler<T>? ValueUpdated;

    /// <inheritdoc/>
    public void Dispose()
    {
        _watcher.Changed -= OnChanged;
        _watcher.Dispose();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
