using System.Text;
using FSBS.Api.Auth;
using FSBS.Api.Endpoints;
using FSBS.Api.Middleware;
using FSBS.Application;
using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure;
using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Authentication ────────────────────────────────────────────────────────────
// Dev scheme: local HMAC-signed JWTs issued by POST /dev/auth/token.
// Real Cognito "Staff" + "Customer" schemes are added in the auth feature step.
if (builder.Environment.IsDevelopment())
{
    var devSecret = builder.Configuration["DevAuth:Secret"]
        ?? throw new InvalidOperationException("DevAuth:Secret must be set in appsettings.Development.json.");
    var devIssuer = builder.Configuration["DevAuth:Issuer"] ?? "fsbs-dev";

    builder.Services.AddAuthentication(defaultScheme: "Dev")
        .AddJwtBearer("Dev", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = devIssuer,
                ValidateAudience         = true,
                ValidAudience            = devIssuer,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(devSecret)),
            };
        });
}

builder.Services.AddAuthorization();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevEndpoints(builder.Configuration);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapInvitationEndpoints();
app.MapOrganisationEndpoints();

app.Run();
