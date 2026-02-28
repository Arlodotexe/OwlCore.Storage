using System;

namespace OwlCore.Storage;

/// <summary>
/// Extends <see cref="IMutableLastModifiedAtProperty"/> to support updating the last modified timestamp.
/// </summary>
/// <remarks>
/// <para>
/// The value passed to <see cref="IModifiableStorageProperty{T}.UpdateValueAsync"/> is non-nullable
/// because most underlying storage systems do not support setting a null or "unset" timestamp value.
/// </para>
/// <para>
/// Property lifecycle is tied to the property itself; there is no "delete" semantic for individual properties.
/// </para>
/// </remarks>
public interface IModifiableLastModifiedAtProperty : IMutableLastModifiedAtProperty, IModifiableStorageProperty<DateTime?>
{
}
