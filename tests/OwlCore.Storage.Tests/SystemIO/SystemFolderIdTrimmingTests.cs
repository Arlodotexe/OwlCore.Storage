using OwlCore.Storage.System.IO;

namespace OwlCore.Storage.Tests.SystemIO;

/// <summary>
/// Verifies that <see cref="SystemFolder"/> never shortens a folder path (and therefore its ID) while trimming trailing
/// separators, on any platform. Unix-like roots, Windows drive roots and UNC share roots all trim differently, and the
/// list separator is never a directory separator.
/// </summary>
[TestClass]
public class SystemFolderIdTrimmingTests
{
    [TestMethod]
    public void RootFolderIdIsTheFullRootPath()
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

            Assert.AreEqual(root, new SystemFolder(root).Id,
                $"A root folder's ID must be the complete root path. Trimming it produces a different path, or an empty one. Root: '{root}'");

            var alternateSeparatorRoot = root.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (Directory.Exists(alternateSeparatorRoot))
                Assert.AreEqual(root, new SystemFolder(alternateSeparatorRoot).Id,
                    $"A root folder's ID must be the complete root path regardless of the separator style it was constructed with. Root: '{alternateSeparatorRoot}'");
        }

        Assert.IsTrue(verifiedCount > 0, "Expected the root of the current platform's temp path to be verifiable.");
    }

    [TestMethod]
    public void FolderPathEndingInListSeparatorKeepsItsFullId()
    {
        // PathSeparator is the list separator (':' on unix-like systems, ';' on Windows) and is never a trailing directory
        // separator, but it used to be trimmed along with them, silently chopping a character off the ID of any folder whose
        // name ends with one.
        var listSeparator = Path.PathSeparator;
        var parentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var folderName = $"FolderNamedWithATrailingListSeparator{listSeparator}";
        var folderPath = Path.Combine(parentPath, folderName);

        Directory.CreateDirectory(folderPath);

        try
        {
            var createdDirectories = new DirectoryInfo(parentPath).GetDirectories();

            Assert.AreEqual(1, createdDirectories.Length, $"Expected exactly one folder to have been created in '{parentPath}'.");
            Assert.AreEqual(folderName, createdDirectories[0].Name,
                $"Expected the filesystem to preserve a folder name ending in '{listSeparator}'.");

            Assert.AreEqual(folderPath, new SystemFolder(folderPath).Id,
                $"A folder ID must keep its final '{listSeparator}' character. Path: '{folderPath}'");

            Assert.AreEqual(folderPath, new SystemFolder(folderPath + Path.DirectorySeparatorChar).Id,
                $"Trailing directory separators may be trimmed, but not at the cost of the '{listSeparator}' before them. Path: '{folderPath}'");
        }
        finally
        {
            Directory.Delete(parentPath, true);
        }
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
