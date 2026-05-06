using FSBS.Domain.Enums;

namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Provides access to the identity of the currently authenticated user, resolved
/// from the request's JWT claims. Registered as a scoped service so each request
/// gets an independent instance.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The application user ID (maps to <c>AppUser.Id</c>), sourced from the
    /// Cognito <c>sub</c> claim. Returns <see cref="Guid.Empty"/> for
    /// unauthenticated requests.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// The tenant the user belongs to. Staff always carry the school's root
    /// tenant GUID. Returns <see cref="Guid.Empty"/> for unauthenticated
    /// requests.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// The organisation the current user belongs to. Populated only for
    /// CorporateManager and CorporateStudent tokens that carry an <c>org_id</c>
    /// claim. Null for staff users and unauthenticated requests.
    /// </summary>
    Guid? OrgId { get; }

    /// <summary>
    /// The role of the current user, normalised from the <c>app_role</c> claim.
    /// Defaults to <see cref="AppRole.PrivateCustomer"/> when the claim is absent.
    /// </summary>
    AppRole Role { get; }

    /// <summary>
    /// Returns true when the current HTTP request carries a valid authenticated
    /// identity; false for anonymous/unauthenticated requests.
    /// </summary>
    bool IsAuthenticated { get; }
}
