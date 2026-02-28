using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="IMutableLastModifiedAtOffsetProperty"/> to support updating the last modified timestamp with timezone offset.
/// </summary>
/// <remarks>
/// The value passed to <see cref="IModifiableStorageProperty{T}.UpdateValueAsync"/> is non-nullable
/// because most underlying storage systems do not support setting a null or "unset" timestamp value.
/// </remarks>
public interface IModifiableLastModifiedAtOffsetProperty : IMutableLastModifiedAtOffsetProperty, IModifiableStorageProperty<DateTimeOffset?>
{
}
