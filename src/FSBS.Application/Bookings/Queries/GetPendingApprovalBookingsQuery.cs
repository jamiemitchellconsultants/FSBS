using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

/// <summary>Returns all bookings in PendingApproval status, for display in the SalesStaff approval queue.</summary>
public record GetPendingApprovalBookingsQuery : IRequest<IReadOnlyList<BookingSummaryDto>>;
