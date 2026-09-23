using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>Last modified timestamp with offset property for memory-backed storage items.</summary>
public sealed class MemoryLastModifiedAtOffsetProperty : SimpleModifiableStorageProperty<DateTimeOffset?>, IModifiableLastModifiedAtOffsetProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryLastModifiedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The storable that this property belongs to. Used to compute the Id.</param>
    /// <param name="dateTimeOffsetValue">Instance that holds a reference to the active in-memory value.</param>
    public MemoryLastModifiedAtOffsetProperty(IStorable owner, DateTimeOffsetValue dateTimeOffsetValue) : base(
            id: owner.Id + "/" + nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            name: nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
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
