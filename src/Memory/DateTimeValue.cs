using System;

namespace OwlCore.Storage.Memory;

/// <summary>
/// Holds a reference to a <see cref="DateTime"/> that can be get and set.
/// </summary>
public class DateTimeValue : IUpdateValue<DateTime?>
{
    /// <summary>
    /// Gets or sets a <see cref="DateTime"/> value.
    /// </summary>
    public required DateTime? Value
    {
        get => field;
        set
        {
            field = value;
            Updated?.Invoke(this, value);
        }
    }

    /// <summary>
    /// An event that's raised when <see cref="Value"/> is updated.
    /// </summary>
    public event EventHandler<DateTime?>? Updated;
}
