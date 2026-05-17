using System.Data;
using FSBS.Domain.Events;
using FSBS.Infrastructure;
using FSBS.Worker;
using FSBS.Worker.Email;
using FSBS.Worker.Messaging;
using FSBS.Worker.Notifications;
using FSBS.Worker.Notifications.Handlers;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

// ── Settings ──────────────────────────────────────────────────────────────────
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("Worker"));

// ── Infrastructure services (AWS SDK clients, SES, SQS, S3, Redis) ───────────
// AddInfrastructure registers IAmazonSQS, IAmazonSimpleEmailService,
// ISesEmailService, ISqsPublisher, IS3Service, IAvailabilityCache, etc.
builder.Services.AddInfrastructure(builder.Configuration);

// ── Database (Dapper — lightweight read queries for user contact lookup) ───────
builder.Services.AddTransient<IDbConnection>(_ =>
{
    var connStr = ResolveConnectionString(builder.Configuration);
    return new NpgsqlConnection(connStr);
});

// ── Notification infrastructure ───────────────────────────────────────────────
builder.Services.AddScoped<IUserLookupService, UserLookupService>();
builder.Services.AddScoped<IMessageDispatcher, MessageDispatcher>();

// ── Notification handlers (one per event type) ────────────────────────────────
builder.Services.AddScoped<INotificationHandler<BookingConfirmedEvent>,  BookingConfirmedHandler>();
builder.Services.AddScoped<INotificationHandler<SlotBookedEvent>,        BookingPendingApprovalHandler>();
builder.Services.AddScoped<INotificationHandler<BookingApprovedEvent>,   BookingApprovedHandler>();
builder.Services.AddScoped<INotificationHandler<BookingRejectedEvent>,   BookingRejectedHandler>();
builder.Services.AddScoped<INotificationHandler<BookingCancelledEvent>,  BookingCancelledHandler>();
builder.Services.AddScoped<INotificationHandler<InvitationIssuedEvent>, InvitationIssuedHandler>();

// ── Hosted services ───────────────────────────────────────────────────────────
// SES template seeder runs once at startup to upsert all email templates.
builder.Services.AddHostedService<SesTemplateSeeder>();
// SQS consumer loop — polls the booking events queue continuously.
builder.Services.AddHostedService<SqsConsumerService>();

var host = builder.Build();
host.Run();

static string ResolveConnectionString(IConfiguration config)
{
    var direct = config["Database:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(direct))
        return direct;

    var host     = config["FSBS_DB_HOST"]     ?? throw new InvalidOperationException("FSBS_DB_HOST is not configured.");
    var port     = config["FSBS_DB_PORT"]     ?? "5432";
    var database = config["FSBS_DB_NAME"]     ?? "fsbs";
    var username = config["FSBS_DB_USERNAME"] ?? throw new InvalidOperationException("FSBS_DB_USERNAME is not configured.");
    var password = config["FSBS_DB_PASSWORD"] ?? throw new InvalidOperationException("FSBS_DB_PASSWORD is not configured.");

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
