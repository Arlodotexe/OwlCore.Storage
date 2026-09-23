using System;

namespace OwlCore.Storage;

/// <summary>
/// An implementation of <see cref="IDisposable"/> that calls an <see cref="Action"/> when disposed.
/// </summary>
internal sealed class DisposableDelegate : IDisposable, IDelegable<Action>
{
    /// <summary>
    /// The inner <see cref="Action"/> that is invoked when <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    public required Action Inner { get; init; }

    /// <inheritdoc />
    public void Dispose() => Inner();
}