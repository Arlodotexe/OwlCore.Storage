namespace OwlCore.Storage
{
    /// <summary>
    /// Indicates that the class is holding a reference to an implementation of <typeparamref name="T"/>, to which public properties, events or methods may be delegated to.
    /// </summary>
    /// <typeparam name="T">The type that is delegated to.</typeparam>
    public interface IDelegable<T>
        where T : class
    {
        /// <summary>
        /// A wrapped implementation which member access can be delegated to.
        /// </summary>
        T Inner { get; }
    }
}
