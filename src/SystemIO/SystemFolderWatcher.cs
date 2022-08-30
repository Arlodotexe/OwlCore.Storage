using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace OwlCore.Storage.SystemIO
{
    /// <summary>
    /// Watches for changes in a System.IO folder.
    /// </summary>
    public class SystemFolderWatcher : IFolderWatcher
    {
        private readonly FileSystemWatcher _watcher;

        /// <summary>
        /// Creates a new instance of <see cref="SystemFolderWatcher"/>.
        /// </summary>
        /// <param name="folder"></param>
        public SystemFolderWatcher(SystemFolder folder)
        {
            Folder = folder;

            _watcher = new FileSystemWatcher(folder.Path);

            AttachEvents(_watcher);
        }

        private void AttachEvents(FileSystemWatcher watcher)
        {
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
        }

        private void DetachEvents(FileSystemWatcher watcher)
        {
            watcher.Created -= OnCreated;
            watcher.Deleted -= OnDeleted;
            watcher.Renamed -= OnRenamed;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            var newItem = CreateStorableFromPath(e.FullPath);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<IStorable> { newItem }));
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            var oldItem = CreateStorableFromPath(e.FullPath, minimalImplementation: true);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<IStorable> { oldItem }));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            var newItem = CreateStorableFromPath(e.FullPath);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<IStorable> { newItem }));

            var oldItem = CreateStorableFromPath(e.OldFullPath, minimalImplementation: true);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<IStorable> { oldItem }));
        }

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <inheritdoc />
        public IMutableFolder Folder { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            DetachEvents(_watcher);
            _watcher.Dispose();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        /// <summary>
        /// Creates an System.IO based instance of <see cref="IStorable"/> given the provided <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to use for the new item.</param>
        /// <param name="minimalImplementation">Indicates whether or not to use a fully functional file/folder implementation, or a minimal implementation of <see cref="IStorable"/>. The latter is needed when an item is removed, since you can't interact with a deleted resource.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Could not determine if the provided path is a file or folder.</exception>
        private static IStorable CreateStorableFromPath(string path, bool minimalImplementation = false)
        {
            if (IsFolder(path))
            {
                if (minimalImplementation)
                    return new SimpleStorableItem(id: path, name: Path.GetDirectoryName(path) ?? throw new ArgumentException($"Could not determine directory name from path {path}"));

                return new SystemFolder(path);
            }

            if (IsFile(path))
            {
                if (minimalImplementation)
                    return new SimpleStorableItem(id: path, name: Path.GetFileName(path));

                return new SystemFile(path);
            }

            throw new ArgumentException($"Could not determine if the provided path is a file or folder. Path: {path}");
        }

        private static bool IsFile(string path) => Path.GetFileName(path) != string.Empty;

        private static bool IsFolder(string path) => path.TrimEnd(Path.PathSeparator, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) == Path.GetDirectoryName(path);
    }
}
