namespace FSBS.Shared.ReferenceData;

public sealed record ReferenceItemDto(string Code, string Label, bool IsActive);

public sealed record AccountStatusDto(string Code, string Label, bool IsActive, bool AllowsBooking);

public sealed record UpsertReferenceItemRequest(string Code, string Label, bool IsActive);

public sealed record UpsertAccountStatusRequest(string Code, string Label, bool IsActive, bool AllowsBooking);
