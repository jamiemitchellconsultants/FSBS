# FSBS.Application.Tests

xUnit unit tests for the application layer. Tests MediatR handlers and services with mocked dependencies.

## Responsibilities

- Test command and query handlers in isolation
- Test `PricingService` discount evaluation logic
- Test `ReconfigurationService` slot insertion and orphan cleanup
- Test FluentValidation validators
- Test MediatR pipeline behaviour ordering

## Conventions

- Mock all infrastructure interfaces (`IBookingRepository`, `IEmailService`, `IAvailabilityCache`, etc.) — do not use real EF Core or AWS clients
- One test class per handler or service
- Use NSubstitute or Moq for mocking
- Arrange/Act/Assert structure
