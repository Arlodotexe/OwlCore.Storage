using System;
using System.IO;

namespace OwlCore.Storage;

/// <summary>
/// The exception that is thrown when attempting to create or recreate a file that already exists.
/// </summary>
public class FileAlreadyExistsException : IOException
{
    const int FileAlreadyExistsHResult = unchecked((int)0x80070050);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAlreadyExistsException"/> class.
    /// </summary>
    public FileAlreadyExistsException(string fileName)
        : base($"(HRESULT:0x{FileAlreadyExistsHResult:X8}) The file {fileName} already exists.")
    {
        HResult = FileAlreadyExistsHResult;
    }
}