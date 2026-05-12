using FSBS.Application.Common.Interfaces;
using FSBS.Shared.LessonLibrary;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Copies a lesson template into a new <c>Lesson</c> row attached to the given
/// module. Optional override fields default to the template's stored values.
/// The new lesson is stamped with <c>SourceTemplateId</c> for provenance.
/// </summary>
public record AttachLessonTemplateToModuleCommand(
    Guid LessonTemplateId,
    Guid ModuleId,
    int SequenceOrder,
    int? MinDurationMins,
    bool? RequiresInstructor,
    bool? IsMandatory) : ICommand<LessonDto>;
