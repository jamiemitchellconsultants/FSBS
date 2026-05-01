# FSBS.Integration.Tests

xUnit integration tests. Exercises the full stack from HTTP request through to a real PostgreSQL database running in Docker.

## Responsibilities

- Test API endpoints end-to-end via `WebApplicationFactory<Program>`
- Verify booking state machine transitions against a real DB (including DB-level constraints)
- Test idempotency key behaviour on `POST /bookings`
- Test multi-tenancy query filters
- Test SignalR hub message dispatch on booking mutations
- Test invitation token flow (create → validate → claim)

## Conventions

- Use `docker-compose` to spin up PostgreSQL 16 before the test run
- Each test class owns its own database schema (or resets state via transactions) to avoid ordering dependencies
- Seed data via EF Core directly — not through the API
- `WebApplicationFactory` overrides replace Redis with an in-memory backplane and SQS with a fake
- No real AWS calls — stub all external services

## Running locally

```bash
docker-compose up -d postgres
dotnet test tests/FSBS.Integration.Tests
```
