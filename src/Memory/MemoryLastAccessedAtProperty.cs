using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.Memory;

/// <summary>Last accessed timestamp property for memory-backed storage items.</summary>
public sealed class MemoryLastAccessedAtProperty : SimpleModifiableStorageProperty<DateTime?>, IModifiableLastAccessedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryLastAccessedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The storable that this property belongs to. Used to compute the Id.</param>
    /// <param name="dateTimeValue">Instance that holds a reference to the active in-memory value.</param>
    public MemoryLastAccessedAtProperty(IStorable owner, DateTimeValue dateTimeValue) : base(
            id: owner.Id + "/" + nameof(ILastAccessedAt.LastAccessedAt),
            name: nameof(ILastAccessedAt.LastAccessedAt),
            getter: () => dateTimeValue.Value,
            setter: v => { dateTimeValue.Value = v; }
    )
    {
        DateTimeValue = dateTimeValue;
    }

    /// <summary>
    /// Holds a reference to a <see cref="DateTime"/> that can be get and set.
    /// </summary>
    public DateTimeValue DateTimeValue { get; }

    /// <inheritdoc/>
    public override Task<IStoragePropertyWatcher<DateTime?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IStoragePropertyWatcher<DateTime?>>(new MemoryPropertyWatcher<DateTime?>(this, DateTimeValue));
    }
}
