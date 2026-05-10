namespace FSBS.Domain.Entities;

/// <summary>
/// Reference table defining the customer classification tiers used to select
/// pricing policies. New tiers can be added by administrators without a code deployment.
/// </summary>
public class CustomerClassRef
{
    /// <summary>Short code used as the FK value in <see cref="Organisation"/> and <see cref="PricingPolicy"/>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether this class is available for selection when creating organisations or pricing policies.</summary>
    public bool IsActive { get; set; } = true;
}
