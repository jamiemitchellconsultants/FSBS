using FSBS.Application.Common.Interfaces;
using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using FSBS.Shared.Payments;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

public sealed class OrganisationAccountRepository(FsbsDbContext db) : IOrganisationAccountRepository
{
    public async Task<OrgAccountDto?> GetAccountAsync(Guid orgId, CancellationToken ct = default)
    {
        var account = await db.OrgAccounts
            .Include(a => a.Organisation)
            .Include(a => a.Payments.Where(p => !p.IsDeleted).OrderByDescending(p => p.PaymentDate).Take(20))
            .FirstOrDefaultAsync(a => a.OrgId == orgId, ct);

        return account is null ? null : MapAccount(account);
    }

    public async Task<PaymentDto> RecordPaymentAsync(
        Guid orgId,
        decimal amountGbp,
        DateOnly paymentDate,
        string paymentMethod,
        string? reference,
        string? notes,
        Guid recordedBy,
        CancellationToken ct = default)
    {
        var account = await db.OrgAccounts
            .FirstOrDefaultAsync(a => a.OrgId == orgId, ct)
            ?? throw new OrganisationNotFoundException(orgId);

        var payment = new AccountPayment
        {
            Id = Guid.NewGuid(),
            OrgAccountId = account.Id,
            OrgId = orgId,
            AmountGbp = amountGbp,
            PaymentDate = paymentDate,
            RecordedBy = recordedBy,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending,
            Reference = reference?.Trim(),
            Notes = notes?.Trim(),
        };

        db.AccountPayments.Add(payment);
        await db.SaveChangesAsync(ct);

        return MapPayment(payment);
    }

    public async Task<PaymentDto> VerifyPaymentAsync(
        Guid orgId, Guid paymentId, Guid verifiedBy, CancellationToken ct = default)
    {
        var payment = await db.AccountPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.OrgId == orgId, ct)
            ?? throw new OrganisationNotFoundException(orgId);

        payment.Status     = PaymentStatus.Verified;
        payment.VerifiedBy = verifiedBy;
        payment.VerifiedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapPayment(payment);
    }

    public async Task<PaymentDto> VoidPaymentAsync(
        Guid orgId, Guid paymentId, string reason, Guid voidedBy, CancellationToken ct = default)
    {
        var payment = await db.AccountPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.OrgId == orgId, ct)
            ?? throw new OrganisationNotFoundException(orgId);

        payment.Status     = PaymentStatus.Voided;
        payment.VoidReason = reason;
        payment.VerifiedBy = voidedBy;
        payment.VerifiedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapPayment(payment);
    }

    private static OrgAccountDto MapAccount(OrgAccount a) =>
        new(
            a.Id,
            a.OrgId,
            a.Organisation.Name,
            a.CreditLimitGbp,
            a.CurrentBalanceGbp,
            a.Status.ToString(),
            a.PaymentTermsDays,
            a.Payments.Select(MapPayment).ToList());

    private static PaymentDto MapPayment(AccountPayment p) =>
        new(
            p.Id,
            p.OrgId,
            p.AmountGbp,
            p.PaymentDate,
            p.PaymentMethod.ToString(),
            p.Status.ToString(),
            p.Reference,
            p.Notes,
            p.CreatedAt);
}



