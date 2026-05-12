using FSBS.Application.Common.Interfaces;
using FSBS.Shared.LessonLibrary;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Toggles the <c>IsActive</c> visibility flag of a lesson template without
/// rewriting any of its fields. Retiring (<c>IsActive=false</c>) hides the
/// template from the library picker while preserving existing attached lessons.
/// </summary>
public record SetLessonTemplateActiveCommand(Guid Id, bool IsActive)
    : ICommand<LessonTemplateDto>;
