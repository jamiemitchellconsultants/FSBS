using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public record GetPendingApprovalBookingsQuery : IRequest<IReadOnlyList<BookingSummaryDto>>;
