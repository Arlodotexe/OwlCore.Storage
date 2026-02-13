using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A simple implementation of <see cref="IStorageProperty{T}"/> that retrieves values via a delegate.
/// </summary>
/// <remarks>
/// This implementation can be used for both in-memory values (pass a closure over a variable)
/// or dynamic backing stores (pass a delegate that reads from the source each time).
/// </remarks>
/// <typeparam name="T">The type of the property value.</typeparam>
public class SimpleStorageProperty<T> : IStorageProperty<T>
{
    private readonly Func<CancellationToken, Task<T>> _asyncGetter;

    /// <summary>
    /// Creates a new instance with a synchronous getter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="getter">A delegate that retrieves the current property value.</param>
    public SimpleStorageProperty(string id, string name, Func<T> getter)
    {
        Id = id;
        Name = name;
        _asyncGetter = ct => Task.FromResult(getter());
    }

    /// <summary>
    /// Creates a new instance with an asynchronous getter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="asyncGetter">A delegate that asynchronously retrieves the current property value.</param>
    public SimpleStorageProperty(string id, string name, Func<CancellationToken, Task<T>> asyncGetter)
    {
        Id = id;
        Name = name;
        _asyncGetter = asyncGetter;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Task<T> GetValueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _asyncGetter(cancellationToken);
    }
}
