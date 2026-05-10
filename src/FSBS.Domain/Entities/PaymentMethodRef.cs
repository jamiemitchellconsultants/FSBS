namespace FSBS.Domain.Entities;

/// <summary>
/// Reference table defining the payment methods accepted for account payments.
/// Methods can be enabled or disabled per environment without a code deployment.
/// </summary>
public class PaymentMethodRef
{
    /// <summary>Short code used as the FK value in <see cref="AccountPayment"/>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether this payment method is currently accepted.</summary>
    public bool IsActive { get; set; } = true;
}
