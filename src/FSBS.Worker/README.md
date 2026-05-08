# FSBS.Worker

.NET 10 background worker service. Consumes booking domain events from the SQS queue and dispatches email notifications via SES.

## Responsibilities

- **`SqsConsumerService`**: long-polling `BackgroundService` that reads from the `fsbs-booking-events` SQS queue (long-poll, 10 messages per receive, 20-second wait). Deletes messages on success; leaves them on the queue on failure so the SQS visibility timeout returns them for retry. After `maxReceiveCount` (3) the DLQ captures them.
- **`MessageDispatcher`**: routes each message to the correct `INotificationHandler<T>` based on the `MessageType` SQS message attribute written by `SqsPublisher`.
- **`INotificationHandler<T>` implementations** under `Notifications/Handlers/`: one handler per domain event type (e.g. `SlotBookedHandler`, `BookingApprovedHandler`, `InvitationIssuedHandler`).
- **`UserLookupService`**: Dapper query against `fsbs.app_users` + `fsbs.user_profiles` to resolve email and display name for notification recipients.
- **`SesTemplateSeeder`**: one-time startup task that registers SES email templates if they do not already exist.

## Handled event types

| SQS `MessageType` | Handler | Email sent |
|---|---|---|
| `SlotBookedEvent` | `SlotBookedHandler` | Booking confirmation to customer |
| `BookingConfirmedEvent` | `BookingConfirmedHandler` | Confirmation to customer |
| `BookingApprovedEvent` | `BookingApprovedHandler` | Approval notification to customer |
| `BookingRejectedEvent` | `BookingRejectedHandler` | Rejection notification with reason to customer |
| `BookingCancelledEvent` | `BookingCancelledHandler` | Cancellation notification to customer |
| `InvitationIssuedEvent` | `InvitationIssuedHandler` | Invitation email with token link to invitee |
| `InvitationClaimedEvent` | `InvitationClaimedHandler` | Welcome email to new user |

## Configuration

Bound from `appsettings.json` / environment variables:

| Key | Description |
|---|---|
| `Worker:BookingEventsQueueUrl` | SQS queue URL (injected by CDK as an ECS environment variable) |
| `Worker:SalesStaffEmail` | Distribution address for PendingApproval alerts |
| `Ses:FromAddress` | Verified SES sending identity |
| `Database:ConnectionString` | PostgreSQL connection string for `UserLookupService` |

## Do not add

- MediatR handlers or application-layer types
- EF Core DbContext (use `IDbConnection` / Dapper for read queries)
- Business logic or domain rules
- Direct HTTP calls to the API
