namespace FSBS.Web.Auth;

public static class BookingAccessEvaluator
{
    public static bool CanAccess(
        string appRole,
        Guid sessionUserId,
        string? sessionOrgId,
        string bookerRole,
        Guid? bookerUserId,
        Guid? bookerOrgId)
    {
        if (IsStaff(appRole))
        {
            return true;
        }

        // Fallback for payloads that do not yet include ownership metadata.
        if (bookerUserId is null && bookerOrgId is null)
        {
            return appRole switch
            {
                "PrivateCustomer" => bookerRole == "PrivateCustomer",
                "CorporateStudent" => bookerRole == "CorporateStudent",
                "InternalStudent" => bookerRole == "InternalStudent",
                "CorporateManager" => bookerRole is "CorporateManager" or "CorporateStudent",
                _ => false
            };
        }

        if (sessionUserId == Guid.Empty)
        {
            return false;
        }

        return appRole switch
        {
            "PrivateCustomer" => bookerUserId == sessionUserId,
            "CorporateStudent" => bookerUserId == sessionUserId,
            "InternalStudent" => bookerUserId == sessionUserId,
            "CorporateManager" =>
                bookerUserId == sessionUserId ||
                (TryParseOrgId(sessionOrgId, out var orgId) && bookerOrgId == orgId),
            _ => false
        };
    }

    private static bool TryParseOrgId(string? orgIdValue, out Guid orgId)
    {
        if (Guid.TryParse(orgIdValue, out orgId))
        {
            return true;
        }

        orgId = Guid.Empty;
        return false;
    }

    private static bool IsStaff(string appRole) => appRole is
        "SystemAdmin" or
        "ScheduleAdmin" or
        "CourseDirector" or
        "Instructor" or
        "Management" or
        "SalesStaff";
}

