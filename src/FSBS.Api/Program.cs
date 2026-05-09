using System.Text;
using FSBS.Api.Auth;
using FSBS.Api.Endpoints;
using FSBS.Api.Hubs;
using FSBS.Api.Middleware;
using FSBS.Application;
using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure;
using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title   = "FSBS API",
            Version = "v1",
            Description = "Flight Simulator Booking System — internal API. " +
                          "In Development use POST /dev/auth/token to obtain a Bearer token, " +
                          "then click Authenticate.",
        };

        // Add Bearer JWT security scheme
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "Paste a token from POST /dev/auth/token",
        };

        // Require Bearer on every operation by default
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddSingleton<IClaimsTransformation, FsbsClaimsTransformation>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── SignalR + Redis backplane ─────────────────────────────────────────────────
var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConn))
{
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConn);
}
else
{
    builder.Services.AddSignalR();
}

// ── Authentication ────────────────────────────────────────────────────────────
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
else
{
    var awsRegion      = builder.Configuration["Cognito:AwsRegion"]
        ?? throw new InvalidOperationException("Cognito:AwsRegion must be configured.");
    var staffPoolId    = builder.Configuration["Cognito:StaffPoolId"]
        ?? throw new InvalidOperationException("Cognito:StaffPoolId must be configured.");
    var staffClientId  = builder.Configuration["Cognito:StaffClientId"]
        ?? throw new InvalidOperationException("Cognito:StaffClientId must be configured.");
    var customerPoolId   = builder.Configuration["Cognito:CustomerPoolId"]
        ?? throw new InvalidOperationException("Cognito:CustomerPoolId must be configured.");
    var customerClientId = builder.Configuration["Cognito:CustomerClientId"]
        ?? throw new InvalidOperationException("Cognito:CustomerClientId must be configured.");

    var staffAuthority    = $"https://cognito-idp.{awsRegion}.amazonaws.com/{staffPoolId}";
    var customerAuthority = $"https://cognito-idp.{awsRegion}.amazonaws.com/{customerPoolId}";

    builder.Services
        .AddAuthentication()
        .AddJwtBearer("Staff", options =>
        {
            options.Authority = staffAuthority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = staffAuthority,
                ValidateAudience         = true,
                ValidAudience            = staffClientId,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
            };
        })
        .AddJwtBearer("Customer", options =>
        {
            options.Authority = customerAuthority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = customerAuthority,
                ValidateAudience         = true,
                ValidAudience            = customerClientId,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
            };
        });
}

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            builder.Environment.IsDevelopment() ? ["Dev"] : ["Staff", "Customer"])
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy("RequireSystemAdmin",    p => p.RequireClaim("app_role", "SystemAdmin"))
    .AddPolicy("RequireScheduleAdmin",  p => p.RequireClaim("app_role", "ScheduleAdmin"))
    .AddPolicy("RequireCourseDirector", p => p.RequireClaim("app_role", "CourseDirector"))
    .AddPolicy("RequireInstructor",     p => p.RequireClaim("app_role", "Instructor"))
    .AddPolicy("RequireManagement",     p => p.RequireClaim("app_role", "Management"))
    .AddPolicy("RequireSalesStaff",     p => p.RequireClaim("app_role", "SalesStaff"))
    .AddPolicy("RequireInternalStudent",   p => p.RequireClaim("app_role", "InternalStudent"))
    .AddPolicy("RequirePrivateCustomer",   p => p.RequireClaim("app_role", "PrivateCustomer"))
    .AddPolicy("RequireCorporateManager",  p => p.RequireClaim("app_role", "CorporateManager"))
    .AddPolicy("RequireCorporateStudent",  p => p.RequireClaim("app_role", "CorporateStudent"))
    .AddPolicy("RequireStaff", p => p.RequireClaim("app_role",
        "SystemAdmin", "ScheduleAdmin", "CourseDirector",
        "Instructor", "Management", "SalesStaff", "InternalStudent"))
    .AddPolicy("RequireApprover", p => p.RequireClaim("app_role",
        "SystemAdmin", "SalesStaff"));

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title           = "FSBS API";
        options.Theme           = ScalarTheme.Purple;
        options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.HttpClient);
        options.Authentication  = new ScalarAuthenticationOptions
        {
            PreferredSecurityScheme = "Bearer",
        };
    });
    app.MapDevEndpoints(builder.Configuration);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapAuthEndpoints();
app.MapInvitationEndpoints();
app.MapOrganisationEndpoints();
app.MapBookingEndpoints();
app.MapSimulatorEndpoints();
app.MapAircraftTypeEndpoints();
app.MapPricingEndpoints();
app.MapHub<AvailabilityHub>("/hubs/availability");

app.Run();

// Exposed so integration tests can target this assembly via WebApplicationFactory<Program>.
public partial class Program;
