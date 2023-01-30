using System.IO.Compression;

namespace OwlCore.Storage.Archive;

/// <summary>
/// A minimum implementation of <see cref="IStorable"/> for use by <see cref="ZipFolder"/>.
/// </summary>
public class SimpleZipStorableItem : IStorable
{
    public SimpleZipStorableItem(string rootId, string id, string path, string name, bool isFolder)
    {
        RootId = rootId;
        Id = isFolder ? NormalizeEnding(id) : id;
        Path = isFolder ? NormalizeEnding(path) : path;
        Name = name;
    }
    
    /// <summary>
    /// Creates an instance of <see cref="SimpleZipStorableItem"/>.
    /// </summary>
    /// <param name="id">The ID of the <see cref="IStorable"/>.</param>
    public SimpleZipStorableItem(string id)
    {
        Id = id;
        Name = System.IO.Path.GetFileName(id);

        int splitIdx = id.IndexOf(ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR);
        if (splitIdx < 0)
        {
            // This must be the root folder
            RootId = id;
            Path = string.Empty;
        }
        else
        {
            RootId = id.Substring(0, splitIdx);
            Path = id.Substring(splitIdx + 1);
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="SimpleZipStorableItem"/>.
    /// </summary>
    /// <param name="storable">The <see cref="IStorable"/> to copy the ID and name from.</param>
    /// <param name="path">The path of the item, relative to the root entry.</param>
    internal SimpleZipStorableItem(IStorable storable, string path)
    {
        Id = storable.Id;
        Name = storable.Name;
        Path = path;
        
        int splitIdx = Id.IndexOf(ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR);
        RootId = splitIdx < 0
            ? Id
            : Id.Substring(0, splitIdx);
    }

    internal SimpleZipStorableItem(string id, string path, bool isFolder)
    {
        Id = isFolder ? NormalizeEnding(id) : id;
        Path = isFolder ? NormalizeEnding(path) : path;
        
        int splitIdx = id.IndexOf(ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR);
        if (splitIdx < 0)
        {
            // This must be the root folder
            Name = RootId = id;
        }
        else
        {
            RootId = id.Substring(0, splitIdx);
            Name = System.IO.Path.GetFileName(path);
        }
    }
    
    /// <summary>
    /// Creates an instance of <see cref="SimpleZipStorableItem"/>.
    /// </summary>
    /// <param name="rootId">The ID of the root entry.</param>
    /// <param name="entry">The ZIP entry.</param>
    internal SimpleZipStorableItem(string rootId, ZipArchiveEntry entry)
    {
        Id = rootId + ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR + entry.FullName;
        RootId = rootId;
        Path = entry.FullName;
        Name = entry.Name;
    }
    
    /// <inheritdoc/>
    public string Id { get; private set; }
    
    /// <summary>
    /// The ID of the root entry in the ZIP archive.
    /// </summary>
    public string RootId { get; private set; }
    
    /// <inheritdoc/>
    public string Name { get; private set; }
    
    /// <summary>
    /// The path of the item, relative to the root entry.
    /// </summary>
    public string Path { get; private set; }

    /// <summary>
    /// Adds or removes the trailing directory separator.
    /// </summary>
    /// <param name="isFolder">Whether to treat the storable as a folder or file.</param>
    public void ChangeStorableType(bool isFolder)
    {
        if (isFolder)
        {
            Path = NormalizeEnding(Path);
            Id = NormalizeEnding(Id);
        }
        else
        {
            if (Path[Path.Length - 1] == ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR)
                Path = Path.Substring(0, Path.Length - 1);
            if (Id[Id.Length - 1] == ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR)
                Id = Id.Substring(0, Id.Length - 1);
        }
    }
    
    /// <summary>
    /// Ensures the given path ends with exactly one directory separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized string with a trailing directory separator.</returns>
    public static string NormalizeEnding(string path)
    {
        return path.Length == 0 || path[path.Length - 1] == ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR
            ? path
            : path + ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR;
    }

    /// <summary>
    /// Creates an instance of <see cref="SimpleZipStorableItem"/>.
    /// </summary>
    /// <param name="parentId">The ID of the parent item.</param>
    /// <param name="name">The name of the item.</param>
    /// <param name="isFolder">Whether this item is a folder.</param>
    public static SimpleZipStorableItem CreateFromParentId(string parentId, string name, bool isFolder)
    {
        string id = NormalizeEnding(parentId) + name;
        if (isFolder) id = NormalizeEnding(id);

        string rootId, path;
        int splitIdx = id.IndexOf(ReadOnlyZipFolder.ZIP_DIRECTORY_SEPARATOR);
        if (splitIdx < 0)
        {
            // This must be the root folder
            rootId = id;
            path = string.Empty;
        }
        else
        {
            rootId = id.Substring(0, splitIdx);
            path = NormalizeEnding(id.Substring(splitIdx + 1));
        }
        
        return new(rootId, id, path, name, isFolder);
    }

    public static SimpleZipStorableItem CreateForRoot(string name)
    {
        return new SimpleZipStorableItem(name, name, string.Empty, name, true);
    }
}