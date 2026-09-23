using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests.SystemIO;

/// <summary>
/// Verifies that <see cref="SystemFolder"/> only ever trims trailing separators for consistency, and never trims a
/// folder path (and therefore its ID) down to nothing. A root directory's ID can consist entirely of the directory
/// separator, where trimming would erase the ID instead of shortening it.
/// </summary>
[TestClass]
public class SystemFolderIdTrimmingTests
{
    [TestMethod]
    public void RootFolderIdIsNeverTrimmedToNothing()
    {
        var roots = new List<string>();

        var tempPathRoot = Path.GetPathRoot(Path.GetTempPath());
        Assert.IsFalse(string.IsNullOrEmpty(tempPathRoot), "Expected the current platform's temp path to have a path root.");
        roots.Add(tempPathRoot!);

        foreach (var root in Environment.GetLogicalDrives())
        {
            if (!roots.Contains(root))
                roots.Add(root);
        }

        // A directory separator on its own is a root folder's entire ID, not a separator to remove.
        var separatorAsRoot = Path.DirectorySeparatorChar.ToString();

        if (!roots.Contains(separatorAsRoot))
            roots.Add(separatorAsRoot);

        var verifiedCount = 0;

        foreach (var root in roots)
        {
            // Roots without media (empty card readers, unplugged drives on Windows) are reported as roots but can't be browsed.
            if (!Directory.Exists(root))
                continue;

            verifiedCount++;

            var id = new SystemFolder(root).Id;

            Assert.IsFalse(string.IsNullOrEmpty(id),
                $"A root folder's ID must never be trimmed down to nothing. Root: '{root}'");

            // Only trailing separators may be removed, and nothing else about the ID may move. This relation holds on
            // every platform without assuming how the current platform spells or classifies a root (drive-relative drive
            // letters, UNC shares, or a bare separator), which is exactly what the fix deliberately left alone.
            Assert.IsTrue(
                root.StartsWith(id, StringComparison.Ordinal) &&
                root.Substring(id.Length).All(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar),
                $"A folder ID must be its path with nothing but trailing separators removed. Root: '{root}' Trimmed ID: '{id}'");

            var alternateSeparatorRoot = root.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // A root made entirely of separators keeps the spelling it was built with, because trimming it would erase the
            // ID rather than shorten it; its alternate-separator spelling is therefore a different ID for the same folder.
            var rootIsSeparatorsOnly = root.All(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);

            if (!rootIsSeparatorsOnly && Directory.Exists(alternateSeparatorRoot))
                Assert.AreEqual(id, new SystemFolder(alternateSeparatorRoot).Id,
                    $"A root folder's ID must not depend on the separator style it was constructed with. Root: '{alternateSeparatorRoot}'");
        }

        Assert.IsTrue(verifiedCount > 0, "Expected the root of the current platform's temp path to be verifiable.");
    }

    [TestMethod]
    public void TrailingDirectorySeparatorsAreTrimmedFromNonRootFolderPaths()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(folderPath);

        try
        {
            Assert.AreEqual(folderPath, new SystemFolder(folderPath + Path.DirectorySeparatorChar).Id);
            Assert.AreEqual(folderPath, new SystemFolder(folderPath + Path.DirectorySeparatorChar + Path.AltDirectorySeparatorChar).Id);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }
}

