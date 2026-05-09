namespace FSBS.Shared.Payments;

public record RecordPaymentRequest(
    decimal AmountGbp,
    DateOnly PaymentDate,
    string PaymentMethod,
    string? Reference,
    string? Notes);

public record PaymentDto(
    Guid PaymentId,
    Guid OrgId,
    decimal AmountGbp,
    DateOnly PaymentDate,
    string PaymentMethod,
    string Status,
    string? Reference,
    string? Notes,
    DateTimeOffset RecordedAt);

public record OrgAccountDto(
    Guid AccountId,
    Guid OrgId,
    string OrgName,
    decimal CreditLimitGbp,
    decimal CurrentBalanceGbp,
    string Status,
    int PaymentTermsDays,
    IReadOnlyList<PaymentDto> RecentPayments);
