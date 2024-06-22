using System;

namespace OwlCore.Storage;

/// <summary>
/// A flag enum that indicates a certain type of storable resource.
/// </summary>
[Flags]
public enum StorableType : byte
{
    /// <summary>
    /// Indicates no storable type.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates a storable file.
    /// </summary>
    File = 1,

    /// <summary>
    /// Indicates a storable folder.
    /// </summary>
    Folder = 2,

    /// <summary>
    /// Indicates all storable types.
    /// </summary>
    All = File | Folder,
}
