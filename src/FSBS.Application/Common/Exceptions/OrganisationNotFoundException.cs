namespace FSBS.Application.Common.Exceptions;

public sealed class OrganisationNotFoundException(Guid orgId)
    : Exception($"Organisation {orgId} was not found.");
