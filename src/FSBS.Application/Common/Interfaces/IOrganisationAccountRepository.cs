using FSBS.Shared.Payments;

namespace FSBS.Application.Common.Interfaces;

public interface IOrganisationAccountRepository
{
    Task<OrgAccountDto?> GetAccountAsync(Guid orgId, CancellationToken ct = default);

    Task<PaymentDto> RecordPaymentAsync(
        Guid orgId,
        decimal amountGbp,
        DateOnly paymentDate,
        string paymentMethod,
        string? reference,
        string? notes,
        Guid recordedBy,
        CancellationToken ct = default);
}

