using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Infrastructure.Persistence;
using FSBS.Shared.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

public sealed class ReferenceDataRepository(FsbsDbContext db) : IReferenceDataRepository
{
    public Task<IReadOnlyList<ReferenceItemDto>> GetCustomerClassesAsync(CancellationToken ct = default) =>
        QueryList(db.CustomerClasses.OrderBy(x => x.Code)
            .Select(x => new ReferenceItemDto(x.Code, x.Label, x.IsActive)), ct);

    public Task<IReadOnlyList<ReferenceItemDto>> GetDiscountTypesAsync(CancellationToken ct = default) =>
        QueryList(db.DiscountTypes.OrderBy(x => x.Code)
            .Select(x => new ReferenceItemDto(x.Code, x.Label, x.IsActive)), ct);

    public Task<IReadOnlyList<ReferenceItemDto>> GetPaymentMethodsAsync(CancellationToken ct = default) =>
        QueryList(db.PaymentMethods.OrderBy(x => x.Code)
            .Select(x => new ReferenceItemDto(x.Code, x.Label, x.IsActive)), ct);

    public async Task<IReadOnlyList<AccountStatusDto>> GetAccountStatusesAsync(CancellationToken ct = default) =>
        await db.AccountStatuses.OrderBy(x => x.Code)
            .Select(x => new AccountStatusDto(x.Code, x.Label, x.IsActive, x.AllowsBooking))
            .ToListAsync(ct);

    public async Task<ReferenceItemDto> UpsertCustomerClassAsync(UpsertReferenceItemRequest r, CancellationToken ct = default)
    {
        var e = await db.CustomerClasses.FirstOrDefaultAsync(x => x.Code == r.Code, ct)
                ?? db.CustomerClasses.Add(new CustomerClassRef { Code = r.Code }).Entity;
        e.Label = r.Label; e.IsActive = r.IsActive;
        await db.SaveChangesAsync(ct);
        return new ReferenceItemDto(e.Code, e.Label, e.IsActive);
    }

    public async Task<ReferenceItemDto> UpsertDiscountTypeAsync(UpsertReferenceItemRequest r, CancellationToken ct = default)
    {
        var e = await db.DiscountTypes.FirstOrDefaultAsync(x => x.Code == r.Code, ct)
                ?? db.DiscountTypes.Add(new DiscountTypeRef { Code = r.Code }).Entity;
        e.Label = r.Label; e.IsActive = r.IsActive;
        await db.SaveChangesAsync(ct);
        return new ReferenceItemDto(e.Code, e.Label, e.IsActive);
    }

    public async Task<ReferenceItemDto> UpsertPaymentMethodAsync(UpsertReferenceItemRequest r, CancellationToken ct = default)
    {
        var e = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == r.Code, ct)
                ?? db.PaymentMethods.Add(new PaymentMethodRef { Code = r.Code }).Entity;
        e.Label = r.Label; e.IsActive = r.IsActive;
        await db.SaveChangesAsync(ct);
        return new ReferenceItemDto(e.Code, e.Label, e.IsActive);
    }

    public async Task<AccountStatusDto> UpsertAccountStatusAsync(UpsertAccountStatusRequest r, CancellationToken ct = default)
    {
        var e = await db.AccountStatuses.FirstOrDefaultAsync(x => x.Code == r.Code, ct)
                ?? db.AccountStatuses.Add(new AccountStatusRef { Code = r.Code }).Entity;
        e.Label = r.Label; e.IsActive = r.IsActive; e.AllowsBooking = r.AllowsBooking;
        await db.SaveChangesAsync(ct);
        return new AccountStatusDto(e.Code, e.Label, e.IsActive, e.AllowsBooking);
    }

    private static async Task<IReadOnlyList<T>> QueryList<T>(IQueryable<T> query, CancellationToken ct) =>
        await query.ToListAsync(ct);
}
