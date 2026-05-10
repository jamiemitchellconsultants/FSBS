namespace FSBS.Domain.Entities;

/// <summary>
/// Reference table defining the types of discount that can be applied to bookings.
/// New discount types can be added by administrators without a code deployment.
/// </summary>
public class DiscountTypeRef
{
    /// <summary>Short code used as the FK value in <see cref="DiscountRule"/> and <see cref="BookingDiscount"/>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether this discount type is currently available for use in pricing rules.</summary>
    public bool IsActive { get; set; } = true;
}
