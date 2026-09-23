using System;

namespace OwlCore.Storage.Memory;

/// <summary>
/// Holds a reference to a <see cref="DateTimeOffset"/> that can be get and set.
/// </summary>
public class DateTimeOffsetValue : IUpdateValue<DateTimeOffset?>
{
    /// <summary>
    /// Gets or sets a <see cref="DateTimeOffset"/> value.
    /// </summary>
    public required DateTimeOffset? Value
    {
        get => field;
        set
        {
            field = value;
            Updated?.Invoke(this, Value);
        }
    }

    /// <summary>
    /// An event that's raised when <see cref="Value"/> is updated.
    /// </summary>
    public event EventHandler<DateTimeOffset?>? Updated;
}
