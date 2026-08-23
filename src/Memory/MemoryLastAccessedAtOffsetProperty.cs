using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>Last accessed timestamp with offset property for memory-backed storage items.</summary>
public sealed class MemoryLastAccessedAtOffsetProperty : SimpleModifiableStorageProperty<DateTimeOffset?>, IModifiableLastAccessedAtOffsetProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryLastAccessedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The storable that this property belongs to. Used to compute the Id.</param>
    /// <param name="dateTimeOffsetValue">Instance that holds a reference to the active in-memory value.</param>
    public MemoryLastAccessedAtOffsetProperty(IStorable owner, DateTimeOffsetValue dateTimeOffsetValue) : base(
            id: owner.Id + "/" + nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
            name: nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
            getter: () => dateTimeOffsetValue.Value,
            setter: v => { dateTimeOffsetValue.Value = v; }
    )
    {
        DateTimeOffsetValue = dateTimeOffsetValue;
    }

    /// <summary>
    /// Holds a reference to a <see cref="DateTimeOffset"/> that can be get and set.
    /// </summary>
    public DateTimeOffsetValue DateTimeOffsetValue { get; }

    /// <inheritdoc/>
    public override Task<IStoragePropertyWatcher<DateTimeOffset?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IStoragePropertyWatcher<DateTimeOffset?>>(new MemoryPropertyWatcher<DateTimeOffset?>(this, DateTimeOffsetValue));
    }
}
