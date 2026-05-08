using System.Text;
using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Bookings;
using FSBS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core read-side implementation of <see cref="IBookingRepository"/>.
/// Uses EF Core projections to return DTOs directly without loading full aggregates.
/// Cursor-based pagination encodes <c>(slotStartAt, bookingId)</c> as a Base64 token.
/// </summary>
internal sealed class BookingRepository(FsbsDbContext db) : IBookingRepository
{
    public async Task<PagedResult<BookingSummaryDto>> GetMyBookingsPageAsync(
        Guid userId, string? afterCursor, int limit, CancellationToken ct)
    {
        var cursorPos = afterCursor is not null ? DecodeCursor(afterCursor) : null;

        var baseQuery = db.Bookings
            .Where(b => b.BookedBy == userId)
            .Select(b => new
            {
                b.Id,
                b.Status,
                b.TrainingType,
                b.BookerRole,
                b.StudentCount,
                b.NetPriceGbp,
                AircraftType = b.Configuration.AircraftType,
                SimulatorUnitName = b.Configuration.SimulatorUnit.Name,
                FirstSlotStartAt = b.Slots.OrderBy(s => s.StartAt).Select(s => (DateTimeOffset?)s.StartAt).FirstOrDefault(),
                FirstSlotEndAt   = b.Slots.OrderBy(s => s.StartAt).Select(s => (DateTimeOffset?)s.EndAt).FirstOrDefault(),
                FirstSlotDurationMins = b.Slots.OrderBy(s => s.StartAt).Select(s => (int?)s.DurationMins).FirstOrDefault(),
                FirstSlotBayName = b.Slots.OrderBy(s => s.StartAt).Select(s => s.Bay.Name).FirstOrDefault(),
                FirstSlotUnitName = b.Slots.OrderBy(s => s.StartAt).Select(s => s.Bay.SimulatorUnit.Name).FirstOrDefault(),
                InstructorFirstName = b.Slots
                    .OrderBy(s => s.StartAt)
                    .Where(s => s.Instructor != null)
                    .Select(s => s.Instructor!.User.Profile!.FirstName)
                    .FirstOrDefault(),
                InstructorLastName = b.Slots
                    .OrderBy(s => s.StartAt)
                    .Where(s => s.Instructor != null)
                    .Select(s => s.Instructor!.User.Profile!.LastName)
                    .FirstOrDefault(),
            });

        if (cursorPos.HasValue)
        {
            var (cursorStartAt, cursorId) = cursorPos.Value;
            baseQuery = baseQuery.Where(x =>
                x.FirstSlotStartAt < cursorStartAt ||
                (x.FirstSlotStartAt == cursorStartAt && x.Id < cursorId));
        }

        var query = baseQuery
            .OrderByDescending(x => x.FirstSlotStartAt)
            .ThenByDescending(x => x.Id);

        var rows = await query.Take(limit + 1).ToListAsync(ct);
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        var items = page.Select(x => new BookingSummaryDto(
            x.Id,
            x.Status.ToString(),
            x.TrainingType.ToString(),
            x.AircraftType,
            x.FirstSlotUnitName ?? x.SimulatorUnitName,
            x.FirstSlotBayName ?? string.Empty,
            x.FirstSlotStartAt ?? default,
            x.FirstSlotEndAt ?? default,
            x.FirstSlotDurationMins ?? 0,
            x.StudentCount,
            x.InstructorFirstName is not null && x.InstructorLastName is not null
                ? $"{x.InstructorFirstName} {x.InstructorLastName}"
                : null,
            x.NetPriceGbp,
            x.BookerRole.ToString()
        )).ToList();

        var nextCursor = hasMore && page.Count > 0
            ? EncodeCursor(page[^1].FirstSlotStartAt ?? default, page[^1].Id)
            : null;

        return new PagedResult<BookingSummaryDto>(items, nextCursor);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetMyBookingsForRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var rows = await db.Bookings
            .Where(b => b.BookedBy == userId &&
                        b.Slots.Any(s => s.StartAt >= from && s.StartAt < to))
            .Select(b => new
            {
                b.Id,
                b.Status,
                b.TrainingType,
                b.BookerRole,
                b.StudentCount,
                b.NetPriceGbp,
                AircraftType = b.Configuration.AircraftType,
                SimulatorUnitName = b.Configuration.SimulatorUnit.Name,
                FirstSlotStartAt = b.Slots.OrderBy(s => s.StartAt).Select(s => (DateTimeOffset?)s.StartAt).FirstOrDefault(),
                FirstSlotEndAt   = b.Slots.OrderBy(s => s.StartAt).Select(s => (DateTimeOffset?)s.EndAt).FirstOrDefault(),
                FirstSlotDurationMins = b.Slots.OrderBy(s => s.StartAt).Select(s => (int?)s.DurationMins).FirstOrDefault(),
                FirstSlotBayName = b.Slots.OrderBy(s => s.StartAt).Select(s => s.Bay.Name).FirstOrDefault(),
                FirstSlotUnitName = b.Slots.OrderBy(s => s.StartAt).Select(s => s.Bay.SimulatorUnit.Name).FirstOrDefault(),
                InstructorFirstName = b.Slots
                    .OrderBy(s => s.StartAt)
                    .Where(s => s.Instructor != null)
                    .Select(s => s.Instructor!.User.Profile!.FirstName)
                    .FirstOrDefault(),
                InstructorLastName = b.Slots
                    .OrderBy(s => s.StartAt)
                    .Where(s => s.Instructor != null)
                    .Select(s => s.Instructor!.User.Profile!.LastName)
                    .FirstOrDefault(),
            })
            .OrderBy(x => x.FirstSlotStartAt)
            .ToListAsync(ct);

        return rows.Select(x => new BookingSummaryDto(
            x.Id,
            x.Status.ToString(),
            x.TrainingType.ToString(),
            x.AircraftType,
            x.FirstSlotUnitName ?? x.SimulatorUnitName,
            x.FirstSlotBayName ?? string.Empty,
            x.FirstSlotStartAt ?? default,
            x.FirstSlotEndAt ?? default,
            x.FirstSlotDurationMins ?? 0,
            x.StudentCount,
            x.InstructorFirstName is not null && x.InstructorLastName is not null
                ? $"{x.InstructorFirstName} {x.InstructorLastName}"
                : null,
            x.NetPriceGbp,
            x.BookerRole.ToString()
        )).ToList();
    }

    public async Task<BookingDetailDto?> GetMyBookingDetailAsync(
        Guid bookingId, Guid userId, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.Configuration).ThenInclude(c => c.SimulatorUnit)
            .Include(b => b.Slots.OrderBy(s => s.StartAt))
                .ThenInclude(s => s.Bay).ThenInclude(bay => bay.SimulatorUnit)
            .Include(b => b.Slots)
                .ThenInclude(s => s.Instructor!.User.Profile)
            .Include(b => b.Approval)
            .Include(b => b.Discounts)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.BookedBy == userId, ct);

        if (booking is null) return null;

        var slots = booking.Slots.Select(s => new BookingSlotDto(
            s.Id,
            s.Bay.SimulatorUnit.Name,
            s.Bay.Name,
            s.Instructor is not null
                ? $"{s.Instructor.User.Profile!.FirstName} {s.Instructor.User.Profile.LastName}"
                : null,
            s.StartAt,
            s.EndAt,
            s.DurationMins,
            s.SlotStatus.ToString()
        )).ToList();

        var approval = booking.Approval is not null
            ? new BookingApprovalDto(
                booking.Approval.Decision.ToString(),
                booking.Approval.ReviewedAt,
                booking.Approval.RejectionReason)
            : null;

        var discounts = booking.Discounts.Select(d => new BookingDiscountDto(
            d.DiscountType.ToString(),
            d.DiscountPct,
            d.DiscountAmountGbp
        )).ToList();

        return new BookingDetailDto(
            booking.Id,
            booking.Status.ToString(),
            booking.TrainingType.ToString(),
            booking.Configuration.AircraftType,
            booking.Configuration.ConfigMode.ToString(),
            booking.Configuration.SimulatorUnit.Name,
            booking.StudentCount,
            booking.GrossPriceGbp,
            booking.DiscountGbp,
            booking.NetPriceGbp,
            booking.DepartmentName,
            booking.BudgetCode,
            booking.BookerRole.ToString(),
            booking.CreatedAt,
            slots,
            approval,
            discounts
        );
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetPendingApprovalAsync(CancellationToken ct)
    {
        var rows = await db.Bookings
            .Where(b => b.Status == FSBS.Domain.Enums.BookingStatus.PendingApproval)
            .Select(b => new
            {
                b.Id,
                b.Status,
                b.TrainingType,
                b.BookerRole,
                b.StudentCount,
                b.NetPriceGbp,
                AircraftType = b.Configuration.AircraftType,
                SimulatorUnitName = b.Configuration.SimulatorUnit.Name,
                FirstSlotStartAt = b.Slots.OrderBy(s => s.StartAt).Select(s => (DateTimeOffset?)s.StartAt).FirstOrDefault(),
                FirstSlotEndAt   = b.Slots.OrderBy(s => s.StartAt).Select(s => (DateTimeOffset?)s.EndAt).FirstOrDefault(),
                FirstSlotDurationMins = b.Slots.OrderBy(s => s.StartAt).Select(s => (int?)s.DurationMins).FirstOrDefault(),
                FirstSlotBayName = b.Slots.OrderBy(s => s.StartAt).Select(s => s.Bay.Name).FirstOrDefault(),
                FirstSlotUnitName = b.Slots.OrderBy(s => s.StartAt).Select(s => s.Bay.SimulatorUnit.Name).FirstOrDefault(),
                InstructorFirstName = b.Slots
                    .OrderBy(s => s.StartAt)
                    .Where(s => s.Instructor != null)
                    .Select(s => s.Instructor!.User.Profile!.FirstName)
                    .FirstOrDefault(),
                InstructorLastName = b.Slots
                    .OrderBy(s => s.StartAt)
                    .Where(s => s.Instructor != null)
                    .Select(s => s.Instructor!.User.Profile!.LastName)
                    .FirstOrDefault(),
            })
            .OrderBy(x => x.FirstSlotStartAt)
            .ToListAsync(ct);

        return rows.Select(x => new BookingSummaryDto(
            x.Id,
            x.Status.ToString(),
            x.TrainingType.ToString(),
            x.AircraftType,
            x.FirstSlotUnitName ?? x.SimulatorUnitName,
            x.FirstSlotBayName ?? string.Empty,
            x.FirstSlotStartAt ?? default,
            x.FirstSlotEndAt ?? default,
            x.FirstSlotDurationMins ?? 0,
            x.StudentCount,
            x.InstructorFirstName is not null && x.InstructorLastName is not null
                ? $"{x.InstructorFirstName} {x.InstructorLastName}"
                : null,
            x.NetPriceGbp,
            x.BookerRole.ToString()
        )).ToList();
    }

    private static string EncodeCursor(DateTimeOffset startAt, Guid bookingId)
    {
        var raw = $"{startAt.UtcTicks}_{bookingId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTimeOffset StartAt, Guid BookingId)? DecodeCursor(string cursor)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var separatorIndex = raw.IndexOf('_');
            if (separatorIndex < 0) return null;
            var ticks = long.Parse(raw[..separatorIndex]);
            var id = Guid.Parse(raw[(separatorIndex + 1)..]);
            return (new DateTimeOffset(ticks, TimeSpan.Zero), id);
        }
        catch
        {
            return null;
        }
    }
}
