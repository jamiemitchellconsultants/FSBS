using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Development-only endpoints. All return 404 unless the application is running
/// in the Development environment. Never deploy with <c>ASPNETCORE_ENVIRONMENT=Development</c>
/// in production.
/// </summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(
        this IEndpointRouteBuilder app,
        IConfiguration config)
    {
        var group = app.MapGroup("/dev")
            .AllowAnonymous()
            .WithTags("Dev");

        // POST /dev/auth/token?email=...&role=...
        group.MapPost("/auth/token", (
            string email = "dev@fsbs.local",
            string role  = "PrivateCustomer",
            string? orgId = null) =>
        {
            var secret = config["DevAuth:Secret"]
                ?? throw new InvalidOperationException("DevAuth:Secret is not configured.");
            var issuer = config["DevAuth:Issuer"] ?? "fsbs-dev";

            var userId   = Guid.NewGuid().ToString();
            var tenantId = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   userId),
                new(JwtRegisteredClaimNames.Email, email),
                new("app_role",   role),
                new("tenant_id",  tenantId),
            };

            if (orgId is not null)
                claims.Add(new Claim("org_id", orgId));

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:            issuer,
                audience:          issuer,
                claims:            claims,
                expires:           DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Results.Ok(new { token = tokenString, userId, tenantId, email, role });
        })
        .WithName("GetDevToken")
        .WithSummary("Issue a signed dev JWT for any role (development only).")
        .WithDescription(
            "Use this endpoint to create temporary test JWTs. Pass email and a valid AppRole (e.g., PrivateCustomer, " +
            "ScheduleAdmin, Instructor). Optionally specify orgId for corporate roles. " +
            "Return value includes the token, generated user ID, and tenant ID for debugging.");

        // POST /dev/users/seed?email=...&role=...
        group.MapPost("/users/seed", async (
            string email,
            FsbsDbContext db,
            string role = "PrivateCustomer",
            string? firstName = null,
            string? lastName  = null,
            CancellationToken ct = default) =>
        {
            if (!Enum.TryParse<AppRole>(role, out var appRole))
                return Results.BadRequest(new { error = $"Unknown role '{role}'." });

            var existing = await db.AppUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email, ct);

            if (existing is not null)
                return Results.Conflict(new
                {
                    message = $"A user with email '{email}' already exists.",
                    userId  = existing.Id
                });

            var userId   = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            db.AppUsers.Add(new AppUser
            {
                Id         = userId,
                TenantId   = tenantId,
                CognitoSub = email,
                Email      = email,
                AppRole    = appRole,
                IsDeleted  = false,
            });

            db.UserProfiles.Add(new UserProfile
            {
                Id        = userId,
                FirstName = firstName ?? string.Empty,
                LastName  = lastName  ?? string.Empty,
            });

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { userId, tenantId, email, role });
        })
        .WithName("SeedDevUser")
        .WithSummary("Create an AppUser + UserProfile row (development only).")
        .WithDescription(
            "Bypasses Cognito and the Post-Confirmation Lambda. Useful for quick local testing. " +
            "Creates a database user record directly. Optionally provide firstName and lastName; " +
            "otherwise they default to empty strings. The response includes the generated user ID and tenant ID.");

        return app;
    }
}
