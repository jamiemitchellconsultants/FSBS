using Fluxor;
using FSBS.Shared.Bookings;

namespace FSBS.Web.State.MyBookings;

public enum BookingViewMode { List, Day, Week, Calendar }

[FeatureState]
public record MyBookingsState
{
    public BookingViewMode ViewMode { get; init; } = BookingViewMode.List;
    public DateOnly SelectedDate { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    public IReadOnlyList<BookingSummaryDto> ListItems { get; init; } = [];
    public string? ListNextCursor { get; init; }
    public bool ListIsLoading { get; init; }
    public bool ListHasMore { get; init; }

    public IReadOnlyList<BookingSummaryDto> RangeItems { get; init; } = [];
    public bool RangeIsLoading { get; init; }

    public string? Error { get; init; }
}
