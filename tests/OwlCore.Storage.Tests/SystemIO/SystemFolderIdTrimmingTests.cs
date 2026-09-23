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

            Assert.IsTrue(Path.IsPathRooted(id),
                $"A root folder's ID must still be a rooted path after trimming. Root: '{root}' Trimmed ID: '{id}'");

            var alternateSeparatorRoot = root.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (Directory.Exists(alternateSeparatorRoot))
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
