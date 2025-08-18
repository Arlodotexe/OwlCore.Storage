using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Extension methods for creating and traversing storage items by relative path.
/// </summary>
public static class CreateRelativeStorageExtensions
{
    /// <summary>
    /// Determines whether the last segment of the provided path parts likely represents a file name.
    /// A trailing slash disables file-tail detection. Uses simple heuristics: contains a '.' and does not end with '.'.
    /// </summary>
    /// <param name="parts">The path segments split by '/'.</param>
    /// <param name="hasTrailingSlash">Whether the original path ended with a slash.</param>
    /// <returns>True if the last segment looks like a file; otherwise false.</returns>
    private static bool LooksLikeFileTail(string[] parts, bool hasTrailingSlash)
    {
        if (hasTrailingSlash || parts == null || parts.Length == 0)
            return false;

        // Use APIs available on all target frameworks to avoid further #if blocks.
        var last = parts[parts.Length - 1];
        return last.Contains(".") && !last.EndsWith(".");
    }

    /// <summary>
    /// Creates an item by traversing a relative path from the provided <see cref="IStorable"/>.
    /// Explicitly specify whether the target is a file or a folder.
    /// Supports "." (current) and ".." (parent) segments. Starting from a child is supported via its parent.
    /// </summary>
    /// <param name="from">The starting item. Must be a folder or a child with a parent to support "..".</param>
    /// <param name="relativePath">The relative path to traverse.</param>
    /// <param name="targetType">Must be either <see cref="StorableType.File"/> or <see cref="StorableType.Folder"/>.</param>
    /// <param name="overwrite">Whether to overwrite existing items when creating.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or existing item at the target path.</returns>
    /// <exception cref="ArgumentException">Invalid path segments or incompatible starting item.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="targetType"/> value.</exception>
    /// <remarks>
    /// Behavior by <paramref name="targetType"/>:
    /// - <see cref="StorableType.File"/>: The last segment is treated as the file name. A trailing '/' is not allowed.
    ///   All preceding segments are created (or reused) as folders. Returns the created file (via the file-specific wrapper) or the item as <see cref="IStorable"/>.
    /// - <see cref="StorableType.Folder"/>: All segments are treated as folders with no heuristics; a file-like last segment (e.g., "name.ext") will be created as a folder.
    ///   If you want file-tail ignoring semantics while yielding intermediate items, use <see cref="CreateAlongRelativePathAsync(IFolder, string, StorableType, bool, CancellationToken)"/> with <see cref="StorableType.Folder"/>.
    /// </remarks>
    // Core implementation (non-extension) used by public overloads.
    private static async Task<IStorable> CreateByRelativePathCoreAsync(IStorable from, string relativePath, StorableType targetType, bool overwrite, CancellationToken cancellationToken)
    {
        if (from is not IModifiableFolder && from is not IStorableChild)
            throw new ArgumentException($"The starting item '{from.Name}' must be a folder or a child with a parent.", nameof(from));

        if (targetType != StorableType.File && targetType != StorableType.Folder)
            throw new ArgumentOutOfRangeException(nameof(targetType), "Only File or Folder are supported.");

        var current = from;
        var normalized = (relativePath ?? string.Empty).Replace('\\', '/');
#if NETSTANDARD2_0
        var parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
#else
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
#endif
        var isFile = targetType == StorableType.File;
        var hasTrailingSlash = normalized.EndsWith("/");

        if (isFile && hasTrailingSlash)
            throw new ArgumentException("File target cannot end with a directory separator.", nameof(relativePath));

        static async Task<IStorable> GoUpAsync(IStorable item, CancellationToken ct)
        {
            if (item is not IStorableChild child)
                throw new ArgumentException($"A parent folder was requested, but '{item.Name}' is not the child of a directory.");

            var parent = await child.GetParentAsync(ct)
                         ?? throw new ArgumentOutOfRangeException(nameof(relativePath), "A parent folder was requested, but the storable item did not return a parent.");
            return parent;
        }

        if (isFile)
        {
#if NETSTANDARD2_0
            if (parts.Length == 0)
                throw new ArgumentException("File target requires a non-empty path.", nameof(relativePath));
            var fileName = parts[parts.Length - 1];
#else
            if (parts.Length == 0)
                throw new ArgumentException("File target requires a non-empty path.", nameof(relativePath));
            var fileName = parts[^1];
#endif
            var folderSegmentsCount = Math.Max(0, parts.Length - 1);

            for (int i = 0; i < folderSegmentsCount; i++)
            {
                var segment = parts[i].Trim();
                if (segment.Length == 0 || segment == ".")
                    continue;

                if (segment == "..")
                {
                    current = await GoUpAsync(current, cancellationToken);
                    continue;
                }

                if (current is not IModifiableFolder f1)
                    throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{segment}'.");

                current = await f1.CreateFolderAsync(segment, overwrite, cancellationToken);
            }

            var last = fileName.Trim();
            if (last.Length == 0 || last == "." || last == "..")
                throw new ArgumentException("Invalid file name in path.", nameof(relativePath));

            if (current is not IModifiableFolder parentFolder)
                throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{last}'.");

            return await parentFolder.CreateFileAsync(last, overwrite, cancellationToken);
        }
        else
        {
            for (int i = 0; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                if (segment.Length == 0 || segment == ".")
                    continue;

                if (segment == "..")
                {
                    current = await GoUpAsync(current, cancellationToken);
                    continue;
                }

                if (current is not IModifiableFolder f2)
                    throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{segment}'.");

                current = await f2.CreateFolderAsync(segment, overwrite, cancellationToken);
            }

            return current;
        }
    }

    /// <summary>
    /// Creates an item by traversing a relative path from the provided <see cref="IFolder"/>.
    /// </summary>
    /// <param name="from">The starting folder.</param>
    /// <param name="relativePath">The relative path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="targetType">The type of item to create at the target: <see cref="StorableType.File"/> or <see cref="StorableType.Folder"/>.</param>
    /// <param name="overwrite">When true, overwrite existing items with the same name where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created or existing item at the target path.</returns>
    public static Task<IStorable> CreateByRelativePathAsync(this IFolder from, string relativePath, StorableType targetType, bool overwrite = false, CancellationToken cancellationToken = default)
        => CreateByRelativePathCoreAsync(from, relativePath, targetType, overwrite, cancellationToken);

    /// <summary>
    /// Creates an item by traversing a relative path from the provided <see cref="IChildFile"/>.
    /// </summary>
    /// <param name="from">The starting child file; its parent will be used when traversing upward ("..").</param>
    /// <param name="relativePath">The relative path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="targetType">The type of item to create at the target: <see cref="StorableType.File"/> or <see cref="StorableType.Folder"/>.</param>
    /// <param name="overwrite">When true, overwrite existing items with the same name where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created or existing item at the target path.</returns>
    public static Task<IStorable> CreateByRelativePathAsync(this IChildFile from, string relativePath, StorableType targetType, bool overwrite = false, CancellationToken cancellationToken = default)
        => CreateByRelativePathCoreAsync(from, relativePath, targetType, overwrite, cancellationToken);

    /// <summary>
    /// Convenience wrapper to create a folder by relative path.
    /// Equivalent to calling CreateByRelativePathAsync with targetType=Folder.
    /// </summary>
    /// <param name="from">The starting folder.</param>
    /// <param name="relativePath">The relative folder path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="overwrite">When true, overwrite existing folders where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created or existing folder at the target path.</returns>
    public static async Task<IChildFolder> CreateFolderByRelativePathAsync(this IFolder from, string relativePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var result = await CreateByRelativePathCoreAsync(from, relativePath, StorableType.Folder, overwrite, cancellationToken);
        if (result is IChildFolder folder)
            return folder;

        throw new InvalidOperationException("Resolved item is not a folder.");
    }

    /// <summary>
    /// Convenience wrapper to create a folder by relative path starting from a child item.
    /// </summary>
    /// <param name="from">The starting child file; its parent will be used when traversing upward ("..").</param>
    /// <param name="relativePath">The relative folder path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="overwrite">When true, overwrite existing folders where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created or existing folder at the target path.</returns>
    public static async Task<IChildFolder> CreateFolderByRelativePathAsync(this IChildFile from, string relativePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var result = await CreateByRelativePathCoreAsync(from, relativePath, StorableType.Folder, overwrite, cancellationToken);
        if (result is IChildFolder folder)
            return folder;

        throw new InvalidOperationException("Resolved item is not a folder.");
    }

    /// <summary>
    /// Convenience wrapper to create a file by relative path.
    /// Equivalent to calling CreateByRelativePathAsync with targetType=File.
    /// The last segment is the file name; a trailing '/' is not allowed.
    /// </summary>
    /// <param name="from">The starting folder.</param>
    /// <param name="relativePath">The relative path to the file to create from <paramref name="from"/>.</param>
    /// <param name="overwrite">When true, overwrite an existing file where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created or existing file at the target path.</returns>
    public static async Task<IChildFile> CreateFileByRelativePathAsync(this IFolder from, string relativePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var result = await CreateByRelativePathCoreAsync(from, relativePath, StorableType.File, overwrite, cancellationToken);
        if (result is IChildFile file)
            return file;

        throw new InvalidOperationException("Resolved item is not a file.");
    }

    /// <summary>
    /// Convenience wrapper to create a file by relative path starting from a child item.
    /// </summary>
    /// <param name="from">The starting child file; its parent will be used when traversing upward ("..").</param>
    /// <param name="relativePath">The relative path to the file to create from <paramref name="from"/>.</param>
    /// <param name="overwrite">When true, overwrite an existing file where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created or existing file at the target path.</returns>
    public static async Task<IChildFile> CreateFileByRelativePathAsync(this IChildFile from, string relativePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var result = await CreateByRelativePathCoreAsync(from, relativePath, StorableType.File, overwrite, cancellationToken);
        if (result is IChildFile file)
            return file;

        throw new InvalidOperationException("Resolved item is not a file.");
    }
    
    /// <summary>
    /// Traverses/creates folders along a relative path and yields each folder in order as it is visited/created.
    /// Supports "." and ".." segments. If the last segment looks like a file (no trailing slash and contains '.'),
    /// it is ignored and only parent folders are processed (intended behavior for folder paths).
    /// </summary>
    /// <param name="from">Starting item (must be a folder or a child with a parent).</param>
    /// <param name="relativePath">The relative path to traverse.</param>
    /// <param name="overwrite">Whether to overwrite existing items when creating folders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async sequence of folders visited/created, in traversal order.</returns>
    public static async IAsyncEnumerable<IFolder> CreateFoldersAlongRelativePathAsync(this IFolder from, string relativePath, bool overwrite = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in CreateAlongRelativePathCoreAsync(from, relativePath, StorableType.Folder, overwrite, cancellationToken))
        {
            if (item is IFolder folder)
                yield return folder;
        }
    }

    /// <summary>
    /// Traverses/creates folders along a relative path and yields each folder (starting from a child item).
    /// </summary>
    /// <param name="from">The starting child file; its parent will be used when traversing upward ("..").</param>
    /// <param name="relativePath">The relative folder path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="overwrite">When true, overwrite existing folders where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async sequence of folders visited/created, in traversal order.</returns>
    public static async IAsyncEnumerable<IFolder> CreateFoldersAlongRelativePathAsync(this IChildFile from, string relativePath, bool overwrite = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in CreateAlongRelativePathCoreAsync(from, relativePath, StorableType.Folder, overwrite, cancellationToken))
        {
            if (item is IFolder folder)
                yield return folder;
        }
    }

    /// <summary>
    /// Traverses and creates along a relative path, yielding each visited/created item in order.
    /// Explicitly specify whether the target is a file or a folder. For files, yields parent folders then the file.
    /// For folders, "." and ".." are supported and a file-like last segment (no trailing slash and contains '.') is ignored,
    /// yielding only folder items.
    /// </summary>
    /// <param name="from">Starting item (must be a folder or a child with a parent).</param>
    /// <param name="relativePath">The relative path to traverse.</param>
    /// <param name="targetType">Must be either <see cref="StorableType.File"/> or <see cref="StorableType.Folder"/>.</param>
    /// <param name="overwrite">Whether to overwrite existing items when creating.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async sequence of items visited/created, in traversal order.</returns>
    private static async IAsyncEnumerable<IStorable> CreateAlongRelativePathCoreAsync(IStorable from, string relativePath, StorableType targetType, bool overwrite,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (from is not IModifiableFolder && from is not IStorableChild)
            throw new ArgumentException($"The starting item '{from.Name}' must be a folder or a child with a parent.", nameof(from));

        if (targetType != StorableType.File && targetType != StorableType.Folder)
            throw new ArgumentOutOfRangeException(nameof(targetType), "Only File or Folder are supported.");

        var current = from;
        var normalized = (relativePath ?? string.Empty).Replace('\\', '/');
#if NETSTANDARD2_0
        var parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
#else
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
#endif
        var isFile = targetType == StorableType.File;
        var hasTrailingSlash = normalized.EndsWith("/");

        if (isFile && hasTrailingSlash)
            throw new ArgumentException("File target cannot end with a directory separator.", nameof(relativePath));

        // Helper to go up one level
        static async Task<IStorable> GoUpAsync(IStorable item, CancellationToken ct)
        {
            if (item is not IStorableChild child)
                throw new ArgumentException($"A parent folder was requested, but '{item.Name}' is not the child of a directory.");

            var parent = await child.GetParentAsync(ct)
                         ?? throw new ArgumentOutOfRangeException(nameof(relativePath), "A parent folder was requested, but the storable item did not return a parent.");
            return parent;
        }

        if (isFile)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                var isLast = i == parts.Length - 1;

                if (segment.Length == 0 || segment == ".")
                    continue;

                if (segment == "..")
                {
                    current = await GoUpAsync(current, cancellationToken);
                    if (current is IFolder upFolder)
                        yield return upFolder;
                    continue;
                }

                if (!isLast)
                {
                    if (current is not IModifiableFolder pf)
                        throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{segment}'.");

                    var next = await pf.CreateFolderAsync(segment, overwrite, cancellationToken);
                    current = next;
                    yield return next;
                }
                else
                {
                    if (segment == "." || segment == ".." || segment.Length == 0)
                        throw new ArgumentException("Invalid file name in path.", nameof(relativePath));

                    if (current is not IModifiableFolder parentFolder)
                        throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{segment}'.");

                    var created = await parentFolder.CreateFileAsync(segment, overwrite, cancellationToken);
                    yield return created;
                }
            }

            // If no parts, nothing yielded; that's an invalid file path (caught above in sync method), but be lenient here: yield starting folder if available
            if (parts.Length == 0 && current is IFolder startFolder)
                yield return startFolder;
        }
        else
        {
            // Folder target: ignore file-like tail and yield each folder
            var lastLooksFile = LooksLikeFileTail(parts, hasTrailingSlash);
            var effectiveLength = lastLooksFile ? Math.Max(0, parts.Length - 1) : parts.Length;

            var any = false;
            for (int i = 0; i < effectiveLength; i++)
            {
                var segment = parts[i].Trim();
                if (segment.Length == 0 || segment == ".")
                    continue;

                if (segment == "..")
                {
                    current = await GoUpAsync(current, cancellationToken);
                    if (current is IFolder upFolder)
                    {
                        yield return upFolder;
                        any = true;
                    }
                    continue;
                }

                if (current is not IModifiableFolder f2)
                    throw new ArgumentException($"The item '{current.Name}' is not a folder and cannot contain '{segment}'.");

                var next = await f2.CreateFolderAsync(segment, overwrite, cancellationToken);
                current = next;
                yield return next;
                any = true;
            }

            if (!any && current is IFolder finalFolder)
                yield return finalFolder;
        }
    }

    /// <summary>
    /// Traverses and creates along a relative path (starting from a folder).
    /// </summary>
    /// <param name="from">The starting folder.</param>
    /// <param name="relativePath">The relative path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="targetType">The type of item to create at the target: <see cref="StorableType.File"/> or <see cref="StorableType.Folder"/>.</param>
    /// <param name="overwrite">When true, overwrite existing items where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async sequence of items visited/created, in traversal order.</returns>
    public static IAsyncEnumerable<IStorable> CreateAlongRelativePathAsync(this IFolder from, string relativePath, StorableType targetType, bool overwrite = false,
        CancellationToken cancellationToken = default)
        => CreateAlongRelativePathCoreAsync(from, relativePath, targetType, overwrite, cancellationToken);

    /// <summary>
    /// Traverses and creates along a relative path (starting from a child item).
    /// </summary>
    /// <param name="from">The starting child file; its parent will be used when traversing upward ("..").</param>
    /// <param name="relativePath">The relative path to traverse from <paramref name="from"/>. Supports "." and "..".</param>
    /// <param name="targetType">The type of item to create at the target: <see cref="StorableType.File"/> or <see cref="StorableType.Folder"/>.</param>
    /// <param name="overwrite">When true, overwrite existing items where supported.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async sequence of items visited/created, in traversal order.</returns>
    public static IAsyncEnumerable<IStorable> CreateAlongRelativePathAsync(this IChildFile from, string relativePath, StorableType targetType, bool overwrite = false,
        CancellationToken cancellationToken = default)
        => CreateAlongRelativePathCoreAsync(from, relativePath, targetType, overwrite, cancellationToken);
}
