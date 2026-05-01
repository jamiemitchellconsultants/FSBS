# FSBS.Application

The use-case layer. Orchestrates domain objects to fulfil application-level operations. All business logic lives in Domain; this layer coordinates it.

## Responsibilities

- **CQRS commands and queries** via MediatR (`IRequest<T>`, `IRequestHandler<,>`)
- **DTOs**: request/response shapes for each use case
- **FluentValidation validators**: one per command/query, run by `ValidationBehaviour`
- **IService interfaces**: contracts for infrastructure services (`IEmailService`, `IPricingService`, `IAvailabilityCache`, etc.)
- **MediatR pipeline behaviours** (registered in order):
  1. `LoggingBehaviour<,>` — structured logging for every request
  2. `ValidationBehaviour<,>` — runs FluentValidation; throws before handler if invalid
  3. `TransactionBehaviour<,>` — wraps commands in a DB transaction

## Folder structure

```
Bookings/Commands/    BookSimulatorSlotCommand, ApproveBookingCommand, RejectBookingCommand, CancelBookingCommand
Bookings/Queries/     GetBookingQuery, ListPendingApprovalsQuery
Bookings/Services/    ReconfigurationService (reconfig slot insertion logic)
Pricing/Services/     PricingService (discount evaluation, quote generation)
Pricing/Queries/      GetPricingQuoteQuery
Invitations/Commands/ CreateInvitationCommand, RevokeInvitationCommand
Invitations/Queries/  ValidateInvitationTokenQuery
Organisations/Commands/ RecordPaymentCommand, VerifyPaymentCommand, VoidPaymentCommand
Organisations/Queries/  GetOrgAccountQuery
Simulators/Queries/   GetSimulatorAvailabilityQuery
Common/Behaviours/    LoggingBehaviour, ValidationBehaviour, TransactionBehaviour
Common/Interfaces/    IEmailService, IAvailabilityCache, ICurrentUser, etc.
```

## Do not add

- EF Core, Dapper, or any direct DB access
- AWS SDK clients (SES, SQS, S3, Cognito)
- ASP.NET Core or Blazor dependencies
- Domain entity constructors or state transitions (those belong in Domain)
