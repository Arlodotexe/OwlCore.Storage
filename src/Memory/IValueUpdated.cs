using System;

namespace OwlCore.Storage.Memory;

internal interface IUpdateValue<T>
{
    T? Value { get; set; }

    /// <summary>
    /// An event that's raised when <see cref="Value"/> is updated.
    /// </summary>
    event EventHandler<T>? Updated;
}
