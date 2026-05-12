using FSBS.Application.Common.Interfaces;
using MediatR;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Soft-deletes a lesson template. Existing attached <c>Lessons</c> are
/// unaffected; templates are a copy source, not a live link.
/// </summary>
public record SoftDeleteLessonTemplateCommand(Guid Id) : ICommand<Unit>;
