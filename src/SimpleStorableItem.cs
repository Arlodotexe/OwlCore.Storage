namespace OwlCore.Storage;

/// <summary>
/// A minimum implementation of <see cref="IStorable"/>.
/// </summary>
/// <remarks>
/// Useful to identify a resource which might not be accessible, such as when it's removed.
/// </remarks>
public sealed class SimpleStorableItem : IStorable
{
    /// <summary>
    /// Creates a new instance of <see cref="SimpleStorableItem"/>.
    /// </summary>
    /// <param name="id">A unique and consistent identifier for this file or folder. This dedicated resource identifier is used to identify the exact file you're pointing to.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    public SimpleStorableItem(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }
}