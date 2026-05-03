namespace FSBS.Application.Common.Exceptions;

public sealed class InvitationNotFoundException()
    : Exception("Invitation not found. The link may be invalid, expired, or already used.");
