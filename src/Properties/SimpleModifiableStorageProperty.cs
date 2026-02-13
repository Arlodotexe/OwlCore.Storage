using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// A simple implementation of <see cref="IModifiableStorageProperty{T}"/> that retrieves and updates values via delegates.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public class SimpleModifiableStorageProperty<T> : SimpleMutableStorageProperty<T>, IModifiableStorageProperty<T>
{
    private readonly Func<T, CancellationToken, Task> _asyncSetter;

    /// <summary>
    /// Creates a new instance with synchronous getter and setter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="getter">A delegate that retrieves the current property value.</param>
    /// <param name="setter">A delegate that sets the property value.</param>
    public SimpleModifiableStorageProperty(string id, string name, Func<T> getter, Action<T> setter)
        : base(id, name, getter)
    {
        _asyncSetter = (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            setter(v);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Creates a new instance with asynchronous getter and setter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="asyncGetter">A delegate that asynchronously retrieves the current property value.</param>
    /// <param name="asyncSetter">A delegate that asynchronously sets the property value.</param>
    public SimpleModifiableStorageProperty(string id, string name, Func<CancellationToken, Task<T>> asyncGetter, Func<T, CancellationToken, Task> asyncSetter)
        : base(id, name, asyncGetter)
    {
        _asyncSetter = asyncSetter;
    }

    /// <summary>
    /// Creates a new instance with synchronous getter and asynchronous setter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="getter">A delegate that retrieves the current property value.</param>
    /// <param name="asyncSetter">A delegate that asynchronously sets the property value.</param>
    public SimpleModifiableStorageProperty(string id, string name, Func<T> getter, Func<T, CancellationToken, Task> asyncSetter)
        : base(id, name, getter)
    {
        _asyncSetter = asyncSetter;
    }

    /// <summary>
    /// Creates a new instance with asynchronous getter and synchronous setter.
    /// </summary>
    /// <param name="id">A unique identifier for this property.</param>
    /// <param name="name">The display name of this property.</param>
    /// <param name="asyncGetter">A delegate that asynchronously retrieves the current property value.</param>
    /// <param name="setter">A delegate that sets the property value.</param>
    public SimpleModifiableStorageProperty(string id, string name, Func<CancellationToken, Task<T>> asyncGetter, Action<T> setter)
        : base(id, name, asyncGetter)
    {
        _asyncSetter = (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            setter(v);
            return Task.CompletedTask;
        };
    }

    /// <inheritdoc/>
    public Task UpdateValueAsync(T newValue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _asyncSetter(newValue, cancellationToken);
    }
}
