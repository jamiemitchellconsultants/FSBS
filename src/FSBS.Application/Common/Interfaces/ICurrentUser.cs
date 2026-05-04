using FSBS.Domain.Enums;

namespace FSBS.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid TenantId { get; }
    /// <summary>
    /// The organisation the current user belongs to. Populated only for
    /// CorporateManager and CorporateStudent tokens that carry an <c>org_id</c>
    /// claim. Null for staff users and unauthenticated requests.
    /// </summary>
    Guid? OrgId { get; }
    AppRole Role { get; }
    bool IsAuthenticated { get; }
}
