namespace OwlCore.Storage;

/// <summary>
/// A simple implementation of <see cref="IStorageProperty{T}"/> that stores a property value.
/// </summary>
/// <typeparam name="T">The type used to store information about properties.</typeparam>
/// <param name="PropValue">The current property value.</param>
public class SimpleStorageProperty<T>(T PropValue) : IStorageProperty<T>
{
    /// <inheritdoc/>
    public T Value => PropValue;
}
