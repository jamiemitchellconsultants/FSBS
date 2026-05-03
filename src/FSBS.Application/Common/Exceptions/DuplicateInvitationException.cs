namespace FSBS.Application.Common.Exceptions;

public sealed class DuplicateInvitationException(string email, Guid orgId)
    : Exception($"A pending invitation for '{email}' to organisation {orgId} already exists.");
