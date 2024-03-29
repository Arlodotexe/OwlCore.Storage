﻿namespace OwlCore.Storage;

/// <summary>
/// Represents a file that resides within a traversable folder structure.
/// </summary>
public interface IChildFile : IFile, IStorableChild
{
}