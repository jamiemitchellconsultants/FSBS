using FSBS.Domain.Enums;

namespace FSBS.Shared.LessonLibrary;

/// <summary>
/// Compact list-row projection used by the library grid. Excludes the full
/// description and per-attach defaults to keep payloads small.
/// </summary>
public record LessonTemplateListItemDto(
    Guid Id,
    string Title,
    TrainingType TrainingType,
    string? Category,
    bool IsActive,
    int UsageCount);
