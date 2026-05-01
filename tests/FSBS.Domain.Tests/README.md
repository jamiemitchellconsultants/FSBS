# FSBS.Domain.Tests

xUnit unit tests for the domain layer. Fast, in-process, no I/O.

## Responsibilities

- Test aggregate root behaviour and state machine transitions (e.g. `Booking` state machine, capacity validation)
- Test value object equality and invariants
- Test domain event emission
- Test business rule enforcement (minimum duration, capacity caps, reviewer-cannot-equal-booker, etc.)

## Conventions

- One test class per aggregate/entity/value object
- No mocking frameworks needed — domain objects have no external dependencies
- Arrange/Act/Assert structure
- Test names follow `MethodName_StateUnderTest_ExpectedBehaviour` pattern
