namespace FSBS.Shared.Bookings;

public record BookingDetailDto(
    Guid Id,
    string Status,
    string TrainingType,
    string AircraftType,
    string ConfigMode,
    string SimulatorUnitName,
    int StudentCount,
    decimal? GrossPriceGbp,
    decimal? DiscountGbp,
    decimal? NetPriceGbp,
    string? DepartmentName,
    string? BudgetCode,
    string BookerRole,
    DateTimeOffset CreatedAt,
    IReadOnlyList<BookingSlotDto> Slots,
    BookingApprovalDto? Approval,
    IReadOnlyList<BookingDiscountDto> Discounts,
    Guid? BookerUserId = null,
    Guid? BookerOrgId = null
);

public record BookingSlotDto(
    Guid Id,
    string SimulatorUnitName,
    string BayName,
    string? InstructorName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int DurationMins,
    string SlotStatus
);

public record BookingApprovalDto(
    string Decision,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason
);

public record BookingDiscountDto(
    string DiscountType,
    decimal DiscountPct,
    decimal DiscountAmountGbp
);
