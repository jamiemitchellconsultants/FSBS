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
    var connStr = builder.Configuration["Database:ConnectionString"]
        ?? throw new InvalidOperationException("Database:ConnectionString is required.");
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
