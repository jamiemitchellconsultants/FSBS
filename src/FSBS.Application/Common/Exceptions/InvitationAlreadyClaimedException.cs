namespace FSBS.Application.Common.Exceptions;

public sealed class InvitationAlreadyClaimedException()
    : Exception("This invitation has already been used to create an account.");
