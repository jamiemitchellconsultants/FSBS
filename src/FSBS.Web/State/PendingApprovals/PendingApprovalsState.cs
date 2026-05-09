using Fluxor;
using FSBS.Shared.Bookings;

namespace FSBS.Web.State.PendingApprovals;

[FeatureState]
public record PendingApprovalsState
{
    public IReadOnlyList<BookingSummaryDto> Items { get; init; } = [];
    public bool IsLoading { get; init; }
    public string? Error { get; init; }
}
