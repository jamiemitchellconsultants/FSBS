# FSBS.Domain

The innermost layer of the Clean Architecture. Contains the core business model — no framework dependencies, no infrastructure references.

## Responsibilities

- **Aggregate roots**: `Booking`, `SimulatorUnit`, `SimulatorConfiguration`, `Organisation`, `Invitation`
- **Entities**: `BookingSlot`, `ReconfigurationSlot`, `BookingDiscount`, `BookingApproval`, `SimulatorBay`, `OrgAccount`
- **Value objects**: `Money`, `IdempotencyKey`, `TokenHash`, `DateRange`
- **Domain events**: `SlotBookedEvent`, `BookingConfirmedEvent`, `BookingRejectedEvent`, `InvitationClaimedEvent`, etc.
- **Domain interfaces**: `IBookingRepository`, `ISimulatorRepository`, `IInvitationRepository`, etc. (implemented in Infrastructure)
- **Domain exceptions**: `BookingConflictException`, `InvalidBookingStateException`, `CapacityExceededException`, etc.
- **Business rules**: booking state machine transitions, capacity caps, reconfiguration slot logic, pricing immutability

## Key rules enforced here

- Minimum booking duration: 240 minutes
- FlightDeck max capacity: 4 students
- CabinCrew max capacity: 10 students
- InternalStudent bookings skip Provisional and go straight to PendingApproval
- Price locked at Confirmed; never recalculated
- Reviewer cannot be the same user as the booker

## Do not add

- EF Core attributes or DbContext references
- MediatR, FluentValidation, or any application-layer types
- AWS SDK or any infrastructure concerns
- ASP.NET Core dependencies
