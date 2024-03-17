using System.IO;

namespace OwlCore.Storage;

/// <summary>
/// The exception that is thrown when attempting to create or recreate a file that already exists.
/// </summary>
public class FileAlreadyExistsException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileAlreadyExistsException"/> class.
    /// </summary>
    public FileAlreadyExistsException(string fileName) : base($"The file {fileName} already exists.")
    {
    }
}