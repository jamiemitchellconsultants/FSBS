using FSBS.Domain.Enums;

namespace FSBS.Application.Common.Exceptions;

public sealed class PricingPolicyNotFoundException(
    Guid configurationId,
    TrainingType trainingType,
    CustomerClass customerClass)
    : Exception(
        $"No pricing policy found for configuration {configurationId}, " +
        $"training type {trainingType}, customer class {customerClass}.");
