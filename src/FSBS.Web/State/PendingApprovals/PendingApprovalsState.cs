using Fluxor;

namespace FSBS.Web.State.PendingApprovals;

[FeatureState]
public record PendingApprovalsState
{
    public IReadOnlyList<object> Items { get; init; } = [];
    public bool IsLoading { get; init; }
    public string? LastCursor { get; init; }
}
