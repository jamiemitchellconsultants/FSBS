namespace FSBS.Domain.ValueObjects;

/// <summary>
/// A client-supplied key that guarantees exactly-once booking creation.
/// Sourced from the <c>Idempotency-Key</c> HTTP header and stored as a unique
/// index on <c>bookings.idempotency_key</c>.
/// </summary>
public record IdempotencyKey(Guid Value)
{
    public static IdempotencyKey New() => new(Guid.NewGuid());

    public static implicit operator Guid(IdempotencyKey key) => key.Value;
    public static implicit operator IdempotencyKey(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
