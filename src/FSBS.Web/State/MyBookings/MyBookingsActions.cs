using FSBS.Shared.Bookings;

namespace FSBS.Web.State.MyBookings;

public record SetBookingViewModeAction(BookingViewMode Mode);
public record SetBookingSelectedDateAction(DateOnly Date);

public record LoadMyBookingsAction(string? AfterCursor = null);
public record MyBookingsLoadedAction(IReadOnlyList<BookingSummaryDto> Items, string? NextCursor, bool Append);

public record LoadMyBookingsForRangeAction(DateTimeOffset From, DateTimeOffset To);
public record MyBookingsForRangeLoadedAction(IReadOnlyList<BookingSummaryDto> Items);

public record MyBookingsLoadErrorAction(string Message);
