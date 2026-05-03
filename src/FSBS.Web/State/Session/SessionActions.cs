namespace FSBS.Web.State.Session;

public record SetSessionAction(Guid UserId, Guid TenantId, string AppRole, string? OrgId);
public record ClearSessionAction;
