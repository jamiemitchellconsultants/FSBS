namespace FSBS.Shared.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor);
