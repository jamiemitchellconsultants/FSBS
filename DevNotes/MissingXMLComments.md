# Audit — public members missing XML doc comments

## Context

Goal: enumerate every `public` class, struct, record, interface, enum, method, and property in the FSBS server-side projects that lacks an XML `///` doc comment. This is a doc-coverage audit, not an implementation plan — output below is the actionable list.

**Scope** (per user selection)
- `FSBS.Domain`
- `FSBS.Application`
- `FSBS.Shared`
- `FSBS.Api`
- `FSBS.Infrastructure.Persistence.Repositories`
- `FSBS.Infrastructure.Persistence.Repositories.Interfaces`

**Strictness** (per user selection: "Strict, but exclude obvious cases")
- Flag any public type/method/property whose immediately preceding non-attribute, non-blank line is not a `///` doc.
- Inherited docs (`<inheritdoc/>`) count as documented.
- Exclusions applied by the detector:
  - `Program.cs Main` (entry point).
  - Record positional parameter lines — only the record declaration itself is checked, not its component parameters (they live in the surrounding type's doc by convention).
  - Test classes, partial Blazor code-behind — out of scope by project filter.

## Detector caveats

- Heuristic regex over text, not a Roslyn analyser. False positives are possible on:
  - Multi-line method signatures where the method name lands on a continuation line (rare).
  - `public partial class Program;` test marker — flagged but harmless to leave as-is.
  - `public override`/`public new` overloads where docs come from the base type — these should get `<inheritdoc/>` to keep the audit clean.
- Two leftover `Class1.cs` files (`FSBS.Domain/Class1.cs`, `FSBS.Shared/Class1.cs`) are likely `dotnet new` scaffolding — recommend **delete** rather than document.

## Summary

**Total undocumented public members: 374**

| Project | Count |
|---|---:|
| FSBS.Application | 228 |
| FSBS.Infrastructure.Persistence.Repositories | 60 |
| FSBS.Domain | 42 |
| FSBS.Shared | 27 |
| FSBS.Api | 16 |
| FSBS.Infrastructure.Persistence.Repositories.Interfaces | 1 |

| Kind | Count |
|---|---:|
| method | 159 |
| class | 112 |
| record | 63 |
| enum | 19 |
| property | 13 |
| interface | 8 |

## Suggested prioritisation

1. **Quick wins (1-line fix)** — 19 undocumented enums in `FSBS.Domain/Enums/`. These are widely referenced; a short summary per enum is high-value, low-effort.
2. **Public contracts** — 8 interfaces and all `FSBS.Shared/**/*.cs` DTO records. These are the explicit public surface; consumers read these first.
3. **CQRS records** — 63 records (mostly in `FSBS.Application/**/Commands` and `Queries`). Many are positional records where one `///` above the record covers the type; component param `<param>` tags optional.
4. **Handler classes + Handle methods** — the bulk of the 112 classes / 159 methods. Lower priority: implementation detail, repetitive.
5. **Repository methods** — 60 in `FSBS.Infrastructure.Persistence.Repositories`. Most implement an interface — preferred fix is `<inheritdoc/>` on the impl + good docs on the interface.
6. **Delete** the two `Class1.cs` files instead of documenting them.

## Full list (grouped by file)

### src/FSBS.Api/Endpoints/AircraftTypeEndpoints.cs
- L17  [method]  public static IEndpointRouteBuilder MapAircraftTypeEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/AuthEndpoints.cs
- L15  [method]  public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/BookingEndpoints.cs
- L20  [method]  public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/DevEndpoints.cs
- L22  [method]  public static IEndpointRouteBuilder MapDevEndpoints(

### src/FSBS.Api/Endpoints/InstructorScheduleEndpoints.cs
- L18  [method]  public static IEndpointRouteBuilder MapInstructorScheduleEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/InvitationEndpoints.cs
- L14  [method]  public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/OrganisationEndpoints.cs
- L15  [method]  public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/PricingEndpoints.cs
- L14  [method]  public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/ReferenceDataEndpoints.cs
- L15  [method]  public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Endpoints/SimulatorEndpoints.cs
- L11  [class]  public static class SimulatorEndpoints
- L13  [method]  public static IEndpointRouteBuilder MapSimulatorEndpoints(this IEndpointRouteBuilder app)
- L361  [record]  public record SimulatorListResponse(IReadOnlyList<SimulatorDetailDto> Items);

### src/FSBS.Api/Endpoints/UserProfileEndpoints.cs
- L8  [class]  public static class UserProfileEndpoints
- L10  [method]  public static IEndpointRouteBuilder MapUserProfileEndpoints(this IEndpointRouteBuilder app)

### src/FSBS.Api/Middleware/GlobalExceptionHandler.cs
- L26  [method]  public async ValueTask<bool> TryHandleAsync(

### src/FSBS.Api/Program.cs
- L214  [class]  public partial class Program;

### src/FSBS.Application/AircraftTypes/Commands/AircraftTypeCommandHandlers.cs
- L9  [class]  public sealed class CreateAircraftTypeHandler(IAircraftTypeRepository repo)
- L12  [method]  public async Task<AircraftTypeDto> Handle(CreateAircraftTypeCommand request, CancellationToken ct)
- L34  [class]  public sealed class UpdateAircraftTypeHandler(IAircraftTypeRepository repo)
- L37  [method]  public async Task<AircraftTypeDto> Handle(UpdateAircraftTypeCommand request, CancellationToken ct)
- L58  [class]  public sealed class DeleteAircraftTypeHandler(IAircraftTypeRepository repo)
- L61  [method]  public async Task<Unit> Handle(DeleteAircraftTypeCommand request, CancellationToken ct)

### src/FSBS.Application/AircraftTypes/Commands/AircraftTypeCommandValidators.cs
- L5  [class]  public sealed class CreateAircraftTypeCommandValidator : AbstractValidator<CreateAircraftTypeCommand>
- L7  [method]  public CreateAircraftTypeCommandValidator()
- L14  [class]  public sealed class UpdateAircraftTypeCommandValidator : AbstractValidator<UpdateAircraftTypeCommand>
- L16  [method]  public UpdateAircraftTypeCommandValidator()
- L24  [class]  public sealed class DeleteAircraftTypeCommandValidator : AbstractValidator<DeleteAircraftTypeCommand>
- L26  [method]  public DeleteAircraftTypeCommandValidator()

### src/FSBS.Application/AircraftTypes/Commands/AircraftTypeCommands.cs
- L7  [record]  public record CreateAircraftTypeCommand(string IcaoCode, string Name) : ICommand<AircraftTypeDto>;
- L9  [record]  public record UpdateAircraftTypeCommand(Guid AircraftTypeId, string IcaoCode, string Name, bool IsActive) : ICommand<AircraftTypeDto>;
- L11  [record]  public record DeleteAircraftTypeCommand(Guid AircraftTypeId) : ICommand<Unit>;

### src/FSBS.Application/AircraftTypes/Queries/ListAircraftTypesHandler.cs
- L7  [class]  public sealed class ListAircraftTypesHandler(IAircraftTypeRepository repo)
- L10  [method]  public async Task<IReadOnlyList<AircraftTypeDto>> Handle(ListAircraftTypesQuery request, CancellationToken ct)

### src/FSBS.Application/AircraftTypes/Queries/ListAircraftTypesQuery.cs
- L6  [record]  public record ListAircraftTypesQuery : IRequest<IReadOnlyList<AircraftTypeDto>>;

### src/FSBS.Application/Auth/Commands/ConfirmPrivateCustomerRegistrationCommandValidator.cs
- L5  [class]  public sealed class ConfirmPrivateCustomerRegistrationCommandValidator
- L8  [method]  public ConfirmPrivateCustomerRegistrationCommandValidator()

### src/FSBS.Application/Auth/Commands/ConfirmPrivateCustomerRegistrationHandler.cs
- L6  [class]  public sealed class ConfirmPrivateCustomerRegistrationHandler(ICognitoService cognito)

### src/FSBS.Application/Auth/Commands/ProcessHostedUiCallbackCommand.cs
- L5  [record]  public record ProcessHostedUiCallbackCommand(
- L10  [record]  public record ProcessHostedUiCallbackResult(

### src/FSBS.Application/Auth/Commands/ProcessHostedUiCallbackHandler.cs
- L6  [class]  public sealed class ProcessHostedUiCallbackHandler(ICognitoHostedUiService hostedUi)
- L9  [method]  public async Task<ProcessHostedUiCallbackResult> Handle(ProcessHostedUiCallbackCommand request, CancellationToken ct)

### src/FSBS.Application/Auth/Commands/RegisterPrivateCustomerCommandValidator.cs
- L5  [class]  public sealed class RegisterPrivateCustomerCommandValidator
- L8  [method]  public RegisterPrivateCustomerCommandValidator()

### src/FSBS.Application/Auth/Commands/RegisterPrivateCustomerHandler.cs
- L6  [class]  public sealed class RegisterPrivateCustomerHandler(ICognitoService cognito)

### src/FSBS.Application/Auth/Commands/ResendConfirmationCodeCommandValidator.cs
- L5  [class]  public sealed class ResendConfirmationCodeCommandValidator
- L8  [method]  public ResendConfirmationCodeCommandValidator()

### src/FSBS.Application/Auth/Commands/ResendConfirmationCodeHandler.cs
- L6  [class]  public sealed class ResendConfirmationCodeHandler(ICognitoService cognito)

### src/FSBS.Application/Bookings/Commands/ApproveBookingCommandValidator.cs
- L5  [class]  public sealed class ApproveBookingCommandValidator : AbstractValidator<ApproveBookingCommand>
- L7  [method]  public ApproveBookingCommandValidator()

### src/FSBS.Application/Bookings/Commands/ApproveBookingHandler.cs
- L14  [class]  public sealed class ApproveBookingHandler(

### src/FSBS.Application/Bookings/Commands/ApproveBookingResult.cs
- L3  [record]  public record ApproveBookingResult(

### src/FSBS.Application/Bookings/Commands/BookSimulatorSlotCommandValidator.cs
- L14  [method]  public BookSimulatorSlotCommandValidator()

### src/FSBS.Application/Bookings/Commands/BookSimulatorSlotHandler.cs
- L13  [class]  public sealed class BookSimulatorSlotHandler(

### src/FSBS.Application/Bookings/Commands/BookingCapacityValidator.cs
- L16  [method]  public BookingCapacityValidator(ICurrentUser currentUser)

### src/FSBS.Application/Bookings/Commands/BookingSlotValidator.cs
- L14  [method]  public BookingSlotValidator()

### src/FSBS.Application/Bookings/Commands/CancelBookingCommandValidator.cs
- L5  [class]  public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
- L7  [method]  public CancelBookingCommandValidator()

### src/FSBS.Application/Bookings/Commands/CancelBookingHandler.cs
- L12  [class]  public sealed class CancelBookingHandler(

### src/FSBS.Application/Bookings/Commands/CancelBookingResult.cs
- L5  [record]  public record CancelBookingResult(Guid BookingId, BookingStatus Status);

### src/FSBS.Application/Bookings/Commands/RejectBookingCommandValidator.cs
- L5  [class]  public sealed class RejectBookingCommandValidator : AbstractValidator<RejectBookingCommand>
- L7  [method]  public RejectBookingCommandValidator()

### src/FSBS.Application/Bookings/Commands/RejectBookingHandler.cs
- L12  [class]  public sealed class RejectBookingHandler(

### src/FSBS.Application/Bookings/Commands/RejectBookingResult.cs
- L3  [record]  public record RejectBookingResult(Guid BookingId);

### src/FSBS.Application/Bookings/Queries/GetBookingDetailHandler.cs
- L9  [class]  public sealed class GetBookingDetailHandler(IBookingRepository bookings, ICurrentUser currentUser)

### src/FSBS.Application/Bookings/Queries/GetMyBookingsForRangeHandler.cs
- L8  [class]  public sealed class GetMyBookingsForRangeHandler(IBookingRepository bookings, ICurrentUser currentUser)

### src/FSBS.Application/Bookings/Queries/GetMyBookingsHandler.cs
- L9  [class]  public sealed class GetMyBookingsHandler(IBookingRepository bookings, ICurrentUser currentUser)

### src/FSBS.Application/Bookings/Queries/GetPendingApprovalBookingsHandler.cs
- L7  [class]  public sealed class GetPendingApprovalBookingsHandler(IBookingRepository bookings)

### src/FSBS.Application/Bookings/Services/IReconfigurationService.cs
- L5  [interface]  public interface IReconfigurationService

### src/FSBS.Application/Bookings/Services/ReconfigurationService.cs
- L6  [class]  public sealed class ReconfigurationService(
- L12  [method]  public async Task<ReconfigurationSlot?> BuildSlotForConfirmedBookingAsync(
- L57  [method]  public async Task RemoveOrphanedSlotsAsync(

### src/FSBS.Application/Common/Exceptions/AircraftTypeAlreadyExistsException.cs
- L3  [class]  public sealed class AircraftTypeAlreadyExistsException(string icaoCode)

### src/FSBS.Application/Common/Exceptions/AircraftTypeNotFoundException.cs
- L3  [class]  public sealed class AircraftTypeNotFoundException(Guid aircraftTypeId)

### src/FSBS.Application/Common/Exceptions/BookingNotFoundException.cs
- L3  [class]  public sealed class BookingNotFoundException(Guid bookingId)

### src/FSBS.Application/Common/Exceptions/DuplicateInvitationException.cs
- L3  [class]  public sealed class DuplicateInvitationException(string email, Guid orgId)

### src/FSBS.Application/Common/Exceptions/InvitationAlreadyClaimedException.cs
- L3  [class]  public sealed class InvitationAlreadyClaimedException()

### src/FSBS.Application/Common/Exceptions/InvitationNotFoundException.cs
- L3  [class]  public sealed class InvitationNotFoundException()

### src/FSBS.Application/Common/Exceptions/OrganisationNotFoundException.cs
- L3  [class]  public sealed class OrganisationNotFoundException(Guid orgId)

### src/FSBS.Application/Common/Exceptions/PricingPolicyNotFoundException.cs
- L5  [class]  public sealed class PricingPolicyNotFoundException(

### src/FSBS.Application/Common/Exceptions/RegistrationEmailAlreadyExistsException.cs
- L10  [property]  public string Email { get; } = email;

### src/FSBS.Application/Common/Exceptions/SimulatorBayNotFoundException.cs
- L3  [class]  public sealed class SimulatorBayNotFoundException(Guid simulatorBayId)

### src/FSBS.Application/Common/Exceptions/SimulatorConfigurationNotFoundException.cs
- L3  [class]  public sealed class SimulatorConfigurationNotFoundException(Guid simulatorConfigurationId)

### src/FSBS.Application/Common/Exceptions/SimulatorUnitNotFoundException.cs
- L3  [class]  public sealed class SimulatorUnitNotFoundException(Guid simulatorUnitId)

### src/FSBS.Application/Common/Interfaces/ICognitoHostedUiService.cs
- L15  [record]  public sealed record CognitoHostedUiCallbackResult(

### src/FSBS.Application/Common/Interfaces/IOrganisationAccountRepository.cs
- L5  [interface]  public interface IOrganisationAccountRepository

### src/FSBS.Application/Common/Interfaces/IReferenceDataRepository.cs
- L5  [interface]  public interface IReferenceDataRepository

### src/FSBS.Application/Common/Interfaces/IUserProfileRepository.cs
- L5  [interface]  public interface IUserProfileRepository

### src/FSBS.Application/InstructorSchedule/Commands/DeleteOverrideCommand.cs
- L11  [class]  public sealed class DeleteOverrideValidator : AbstractValidator<DeleteOverrideCommand>
- L13  [method]  public DeleteOverrideValidator()
- L20  [class]  public sealed class DeleteOverrideHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
- L23  [method]  public async Task<Unit> Handle(DeleteOverrideCommand request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Commands/SetSingleDayCommand.cs
- L21  [class]  public sealed class SetSingleDayValidator : AbstractValidator<SetSingleDayCommand>
- L23  [method]  public SetSingleDayValidator()
- L40  [class]  public sealed class SetSingleDayHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
- L43  [method]  public async Task<Unit> Handle(SetSingleDayCommand request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Commands/UpsertOverrideCommand.cs
- L24  [class]  public sealed class UpsertOverrideValidator : AbstractValidator<UpsertOverrideCommand>
- L26  [method]  public UpsertOverrideValidator()
- L36  [class]  public sealed class UpsertOverrideHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
- L39  [method]  public async Task<AvailabilityOverrideDto> Handle(UpsertOverrideCommand request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Commands/UpsertWeeklyPatternCommand.cs
- L19  [class]  public sealed class UpsertWeeklyPatternValidator : AbstractValidator<UpsertWeeklyPatternCommand>
- L21  [method]  public UpsertWeeklyPatternValidator()
- L40  [class]  public sealed class UpsertWeeklyPatternHandler(IInstructorScheduleRepository repo, ICurrentUser currentUser)
- L43  [method]  public async Task<WeeklyPatternDto> Handle(UpsertWeeklyPatternCommand request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Queries/GetInstructorScheduleQuery.cs
- L19  [class]  public sealed class GetInstructorScheduleValidator : AbstractValidator<GetInstructorScheduleQuery>
- L21  [method]  public GetInstructorScheduleValidator()
- L31  [class]  public sealed class GetInstructorScheduleHandler(
- L36  [method]  public async Task<InstructorScheduleDto> Handle(GetInstructorScheduleQuery request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Queries/GetMyInstructorScheduleQuery.cs
- L17  [class]  public sealed class GetMyInstructorScheduleValidator : AbstractValidator<GetMyInstructorScheduleQuery>
- L19  [method]  public GetMyInstructorScheduleValidator()
- L28  [class]  public sealed class GetMyInstructorScheduleHandler(
- L34  [method]  public async Task<InstructorScheduleDto> Handle(GetMyInstructorScheduleQuery request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Queries/ListInstructorsQuery.cs
- L13  [class]  public sealed class ListInstructorsHandler(IInstructorRepository repo)
- L16  [method]  public async Task<IReadOnlyList<InstructorRowDto>> Handle(ListInstructorsQuery request, CancellationToken ct)

### src/FSBS.Application/InstructorSchedule/Services/InstructorScheduleResolver.cs
- L33  [method]  public static IReadOnlyList<EffectiveIntervalDto> Resolve(

### src/FSBS.Application/Invitations/Commands/ClaimInvitationCommandValidator.cs
- L5  [class]  public sealed class ClaimInvitationCommandValidator : AbstractValidator<ClaimInvitationCommand>
- L7  [method]  public ClaimInvitationCommandValidator()

### src/FSBS.Application/Invitations/Commands/ClaimInvitationHandler.cs
- L10  [class]  public sealed class ClaimInvitationHandler(IInvitationRepository invitations)

### src/FSBS.Application/Invitations/Commands/CreateCorporateManagerInvitationCommandValidator.cs
- L5  [class]  public sealed class CreateCorporateManagerInvitationCommandValidator
- L8  [method]  public CreateCorporateManagerInvitationCommandValidator()

### src/FSBS.Application/Invitations/Commands/CreateCorporateManagerInvitationHandler.cs
- L12  [class]  public sealed class CreateCorporateManagerInvitationHandler(

### src/FSBS.Application/Invitations/Commands/InviteCorporateStudentCommandValidator.cs
- L5  [class]  public sealed class InviteCorporateStudentCommandValidator
- L8  [method]  public InviteCorporateStudentCommandValidator()

### src/FSBS.Application/Invitations/Commands/InviteCorporateStudentHandler.cs
- L12  [class]  public sealed class InviteCorporateStudentHandler(

### src/FSBS.Application/Invitations/Queries/ValidateInvitationTokenHandler.cs
- L7  [class]  public sealed class ValidateInvitationTokenHandler(IInvitationRepository invitations)

### src/FSBS.Application/Organisations/Commands/PaymentVerifyVoidCommands.cs
- L7  [record]  public record VerifyOrganisationPaymentCommand(Guid OrgId, Guid PaymentId)
- L10  [record]  public record VoidOrganisationPaymentCommand(Guid OrgId, Guid PaymentId, string Reason)
- L13  [class]  public sealed class VerifyOrganisationPaymentCommandValidator
- L16  [method]  public VerifyOrganisationPaymentCommandValidator()
- L23  [class]  public sealed class VoidOrganisationPaymentCommandValidator
- L26  [method]  public VoidOrganisationPaymentCommandValidator()

### src/FSBS.Application/Organisations/Commands/PaymentVerifyVoidHandlers.cs
- L7  [class]  public sealed class VerifyOrganisationPaymentHandler(
- L12  [method]  public Task<PaymentDto> Handle(VerifyOrganisationPaymentCommand request, CancellationToken ct) =>
- L16  [class]  public sealed class VoidOrganisationPaymentHandler(
- L21  [method]  public Task<PaymentDto> Handle(VoidOrganisationPaymentCommand request, CancellationToken ct) =>

### src/FSBS.Application/Organisations/Commands/RecordOrganisationPaymentCommand.cs
- L6  [record]  public record RecordOrganisationPaymentCommand(

### src/FSBS.Application/Organisations/Commands/RecordOrganisationPaymentCommandValidator.cs
- L5  [class]  public sealed class RecordOrganisationPaymentCommandValidator : AbstractValidator<RecordOrganisationPaymentCommand>
- L10  [method]  public RecordOrganisationPaymentCommandValidator()

### src/FSBS.Application/Organisations/Commands/RecordOrganisationPaymentHandler.cs
- L6  [class]  public sealed class RecordOrganisationPaymentHandler(
- L11  [method]  public Task<FSBS.Shared.Payments.PaymentDto> Handle(

### src/FSBS.Application/Organisations/Queries/GetOrganisationAccountHandler.cs
- L7  [class]  public sealed class GetOrganisationAccountHandler(IOrganisationAccountRepository accounts)
- L10  [method]  public Task<OrgAccountDto?> Handle(GetOrganisationAccountQuery request, CancellationToken ct) =>

### src/FSBS.Application/Organisations/Queries/GetOrganisationAccountQuery.cs
- L6  [record]  public record GetOrganisationAccountQuery(Guid OrgId) : IRequest<OrgAccountDto?>;

### src/FSBS.Application/Organisations/Queries/ListOrganisationsHandler.cs
- L6  [class]  public sealed class ListOrganisationsHandler(IOrganisationRepository organisations)

### src/FSBS.Application/Pricing/Services/IPricingService.cs
- L3  [interface]  public interface IPricingService

### src/FSBS.Application/Pricing/Services/PricingService.cs
- L10  [class]  public sealed class PricingService(IPricingPolicyRepository pricingPolicyRepository) : IPricingService
- L12  [method]  public async Task<PricingResult> CalculateAsync(PricingRequest request, CancellationToken ct = default)

### src/FSBS.Application/ReferenceData/Commands/UpsertReferenceDataCommands.cs
- L6  [record]  public record UpsertCustomerClassCommand(UpsertReferenceItemRequest Item)  : ICommand<ReferenceItemDto>;
- L7  [record]  public record UpsertDiscountTypeCommand(UpsertReferenceItemRequest Item)   : ICommand<ReferenceItemDto>;
- L8  [record]  public record UpsertPaymentMethodCommand(UpsertReferenceItemRequest Item)  : ICommand<ReferenceItemDto>;
- L9  [record]  public record UpsertAccountStatusCommand(UpsertAccountStatusRequest Item)  : ICommand<AccountStatusDto>;

### src/FSBS.Application/ReferenceData/Commands/UpsertReferenceDataHandlers.cs
- L7  [class]  public sealed class UpsertCustomerClassHandler(IReferenceDataRepository repo)
- L10  [method]  public Task<ReferenceItemDto> Handle(UpsertCustomerClassCommand request, CancellationToken ct) =>
- L14  [class]  public sealed class UpsertDiscountTypeHandler(IReferenceDataRepository repo)
- L17  [method]  public Task<ReferenceItemDto> Handle(UpsertDiscountTypeCommand request, CancellationToken ct) =>
- L21  [class]  public sealed class UpsertPaymentMethodHandler(IReferenceDataRepository repo)
- L24  [method]  public Task<ReferenceItemDto> Handle(UpsertPaymentMethodCommand request, CancellationToken ct) =>
- L28  [class]  public sealed class UpsertAccountStatusHandler(IReferenceDataRepository repo)
- L31  [method]  public Task<AccountStatusDto> Handle(UpsertAccountStatusCommand request, CancellationToken ct) =>

### src/FSBS.Application/ReferenceData/Commands/UpsertReferenceDataValidators.cs
- L5  [class]  public sealed class UpsertCustomerClassCommandValidator : AbstractValidator<UpsertCustomerClassCommand>
- L7  [method]  public UpsertCustomerClassCommandValidator()
- L14  [class]  public sealed class UpsertDiscountTypeCommandValidator : AbstractValidator<UpsertDiscountTypeCommand>
- L16  [method]  public UpsertDiscountTypeCommandValidator()
- L23  [class]  public sealed class UpsertPaymentMethodCommandValidator : AbstractValidator<UpsertPaymentMethodCommand>
- L25  [method]  public UpsertPaymentMethodCommandValidator()
- L32  [class]  public sealed class UpsertAccountStatusCommandValidator : AbstractValidator<UpsertAccountStatusCommand>
- L34  [method]  public UpsertAccountStatusCommandValidator()

### src/FSBS.Application/ReferenceData/Queries/GetReferenceDataHandlers.cs
- L7  [class]  public sealed class GetCustomerClassesHandler(IReferenceDataRepository repo)
- L10  [method]  public Task<IReadOnlyList<ReferenceItemDto>> Handle(GetCustomerClassesQuery _, CancellationToken ct) =>
- L14  [class]  public sealed class GetDiscountTypesHandler(IReferenceDataRepository repo)
- L17  [method]  public Task<IReadOnlyList<ReferenceItemDto>> Handle(GetDiscountTypesQuery _, CancellationToken ct) =>
- L21  [class]  public sealed class GetPaymentMethodsHandler(IReferenceDataRepository repo)
- L24  [method]  public Task<IReadOnlyList<ReferenceItemDto>> Handle(GetPaymentMethodsQuery _, CancellationToken ct) =>
- L28  [class]  public sealed class GetAccountStatusesHandler(IReferenceDataRepository repo)
- L31  [method]  public Task<IReadOnlyList<AccountStatusDto>> Handle(GetAccountStatusesQuery _, CancellationToken ct) =>

### src/FSBS.Application/ReferenceData/Queries/GetReferenceDataQueries.cs
- L6  [record]  public record GetCustomerClassesQuery : IRequest<IReadOnlyList<ReferenceItemDto>>;
- L7  [record]  public record GetDiscountTypesQuery   : IRequest<IReadOnlyList<ReferenceItemDto>>;
- L8  [record]  public record GetPaymentMethodsQuery  : IRequest<IReadOnlyList<ReferenceItemDto>>;
- L9  [record]  public record GetAccountStatusesQuery : IRequest<IReadOnlyList<AccountStatusDto>>;

### src/FSBS.Application/Simulators/Commands/SimulatorCommandHandlers.cs
- L11  [class]  public sealed class CreateSimulatorUnitHandler(ISimulatorRepository simulators)
- L14  [method]  public async Task<SimulatorDetailDto> Handle(CreateSimulatorUnitCommand request, CancellationToken ct)
- L32  [class]  public sealed class UpdateSimulatorUnitHandler(ISimulatorRepository simulators)
- L35  [method]  public async Task<SimulatorDetailDto> Handle(UpdateSimulatorUnitCommand request, CancellationToken ct)
- L51  [class]  public sealed class DeleteSimulatorUnitHandler(ISimulatorRepository simulators)
- L54  [method]  public async Task<Unit> Handle(DeleteSimulatorUnitCommand request, CancellationToken ct)
- L64  [class]  public sealed class CreateSimulatorBayHandler(ISimulatorRepository simulators)
- L67  [method]  public async Task<SimulatorBayDto> Handle(CreateSimulatorBayCommand request, CancellationToken ct)
- L86  [class]  public sealed class UpdateSimulatorBayHandler(ISimulatorRepository simulators)
- L89  [method]  public async Task<SimulatorBayDto> Handle(UpdateSimulatorBayCommand request, CancellationToken ct)
- L108  [class]  public sealed class DeleteSimulatorBayHandler(ISimulatorRepository simulators)
- L111  [method]  public async Task<Unit> Handle(DeleteSimulatorBayCommand request, CancellationToken ct)
- L124  [class]  public sealed class CreateSimulatorConfigurationHandler(
- L129  [method]  public async Task<SimulatorConfigurationDto> Handle(CreateSimulatorConfigurationCommand request, CancellationToken ct)
- L161  [class]  public sealed class UpdateSimulatorConfigurationHandler(
- L166  [method]  public async Task<SimulatorConfigurationDto> Handle(UpdateSimulatorConfigurationCommand request, CancellationToken ct)
- L195  [class]  public sealed class DeleteSimulatorConfigurationHandler(ISimulatorRepository simulators)
- L198  [method]  public async Task<Unit> Handle(DeleteSimulatorConfigurationCommand request, CancellationToken ct)
- L213  [method]  public static List<TrainingType> ParseTrainingTypes(IReadOnlyList<string> raw)

### src/FSBS.Application/Simulators/Commands/SimulatorCommandValidators.cs
- L5  [class]  public sealed class CreateSimulatorUnitCommandValidator : AbstractValidator<CreateSimulatorUnitCommand>
- L7  [method]  public CreateSimulatorUnitCommandValidator()
- L15  [class]  public sealed class UpdateSimulatorUnitCommandValidator : AbstractValidator<UpdateSimulatorUnitCommand>
- L17  [method]  public UpdateSimulatorUnitCommandValidator()
- L26  [class]  public sealed class DeleteSimulatorUnitCommandValidator : AbstractValidator<DeleteSimulatorUnitCommand>
- L28  [method]  public DeleteSimulatorUnitCommandValidator() => RuleFor(x => x.SimulatorUnitId).NotEmpty();
- L31  [class]  public sealed class CreateSimulatorBayCommandValidator : AbstractValidator<CreateSimulatorBayCommand>
- L33  [method]  public CreateSimulatorBayCommandValidator()
- L40  [class]  public sealed class UpdateSimulatorBayCommandValidator : AbstractValidator<UpdateSimulatorBayCommand>
- L42  [method]  public UpdateSimulatorBayCommandValidator()
- L51  [class]  public sealed class DeleteSimulatorBayCommandValidator : AbstractValidator<DeleteSimulatorBayCommand>
- L53  [method]  public DeleteSimulatorBayCommandValidator()
- L60  [class]  public sealed class CreateSimulatorConfigurationCommandValidator : AbstractValidator<CreateSimulatorConfigurationCommand>
- L62  [method]  public CreateSimulatorConfigurationCommandValidator()
- L72  [class]  public sealed class UpdateSimulatorConfigurationCommandValidator : AbstractValidator<UpdateSimulatorConfigurationCommand>
- L74  [method]  public UpdateSimulatorConfigurationCommandValidator()
- L85  [class]  public sealed class DeleteSimulatorConfigurationCommandValidator : AbstractValidator<DeleteSimulatorConfigurationCommand>
- L87  [method]  public DeleteSimulatorConfigurationCommandValidator()

### src/FSBS.Application/Simulators/Commands/SimulatorCommands.cs
- L7  [record]  public record CreateSimulatorUnitCommand(
- L14  [record]  public record UpdateSimulatorUnitCommand(
- L23  [record]  public record DeleteSimulatorUnitCommand(Guid SimulatorUnitId) : ICommand<Unit>;
- L25  [record]  public record CreateSimulatorBayCommand(
- L30  [record]  public record UpdateSimulatorBayCommand(
- L37  [record]  public record DeleteSimulatorBayCommand(
- L41  [record]  public record CreateSimulatorConfigurationCommand(
- L50  [record]  public record UpdateSimulatorConfigurationCommand(
- L61  [record]  public record DeleteSimulatorConfigurationCommand(

### src/FSBS.Application/Simulators/Queries/GetSimulatorDetailHandler.cs
- L7  [class]  public sealed class GetSimulatorDetailHandler(ISimulatorRepository simulators)
- L10  [method]  public async Task<SimulatorDetailDto?> Handle(GetSimulatorDetailQuery request, CancellationToken ct)

### src/FSBS.Application/Simulators/Queries/GetSimulatorDetailQuery.cs
- L6  [record]  public record GetSimulatorDetailQuery(Guid SimulatorUnitId) : IRequest<SimulatorDetailDto?>;

### src/FSBS.Application/Simulators/Queries/ListSimulatorsHandler.cs
- L7  [class]  public sealed class ListSimulatorsHandler(ISimulatorRepository simulators)
- L10  [method]  public async Task<IReadOnlyList<SimulatorDetailDto>> Handle(ListSimulatorsQuery request, CancellationToken ct)

### src/FSBS.Application/Simulators/Queries/ListSimulatorsQuery.cs
- L6  [record]  public record ListSimulatorsQuery : IRequest<IReadOnlyList<SimulatorDetailDto>>;

### src/FSBS.Application/Simulators/SimulatorDtoMapper.cs
- L8  [method]  public static SimulatorDetailDto ToDetail(SimulatorUnit unit) =>
- L26  [method]  public static SimulatorBayDto ToBay(SimulatorBay bay) =>
- L29  [method]  public static SimulatorConfigurationDto ToConfiguration(SimulatorConfiguration configuration) =>

### src/FSBS.Application/UserProfiles/Commands/UpdateMyProfileCommand.cs
- L7  [record]  public record UpdateMyProfileCommand(UpdateUserProfileRequest Profile) : ICommand<Unit>;

### src/FSBS.Application/UserProfiles/Commands/UpdateMyProfileCommandValidator.cs
- L5  [class]  public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
- L7  [method]  public UpdateMyProfileCommandValidator()

### src/FSBS.Application/UserProfiles/Commands/UpdateMyProfileHandler.cs
- L7  [class]  public sealed class UpdateMyProfileHandler(IUserProfileRepository profiles, ICurrentUser currentUser)
- L10  [method]  public async Task<Unit> Handle(UpdateMyProfileCommand request, CancellationToken ct)

### src/FSBS.Application/UserProfiles/Queries/GetMyProfileHandler.cs
- L7  [class]  public sealed class GetMyProfileHandler(
- L13  [method]  public async Task<UserProfileDto?> Handle(GetMyProfileQuery request, CancellationToken ct)

### src/FSBS.Application/UserProfiles/Queries/GetMyProfileQuery.cs
- L6  [record]  public record GetMyProfileQuery : IRequest<UserProfileDto?>;

### src/FSBS.Application/UserProfiles/Queries/GetPhotoUploadUrlHandler.cs
- L7  [class]  public sealed class GetPhotoUploadUrlHandler(ICurrentUser currentUser, IS3Service s3)
- L10  [method]  public async Task<PhotoUploadUrlResponse> Handle(GetPhotoUploadUrlQuery request, CancellationToken ct)

### src/FSBS.Application/UserProfiles/Queries/GetPhotoUploadUrlQuery.cs
- L6  [record]  public record GetPhotoUploadUrlQuery(string ContentType) : IRequest<PhotoUploadUrlResponse>;

### src/FSBS.Domain/Class1.cs
- L3  [class]  public class Class1   *(recommend delete — `dotnet new` leftover)*

### src/FSBS.Domain/Entities/AggregateRoot.cs
- L14  [property]  public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
- L16  [method]  public void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
- L18  [method]  public void ClearDomainEvents() => _domainEvents.Clear();

### src/FSBS.Domain/Enums/AccountStatus.cs
- L3  [enum]  public enum AccountStatus

### src/FSBS.Domain/Enums/AppRole.cs
- L3  [enum]  public enum AppRole

### src/FSBS.Domain/Enums/ApprovalDecision.cs
- L3  [enum]  public enum ApprovalDecision

### src/FSBS.Domain/Enums/AvailabilityType.cs
- L3  [enum]  public enum AvailabilityType

### src/FSBS.Domain/Enums/BayStatus.cs
- L3  [enum]  public enum BayStatus

### src/FSBS.Domain/Enums/BookingStatus.cs
- L3  [enum]  public enum BookingStatus

### src/FSBS.Domain/Enums/ConfigurationMode.cs
- L3  [enum]  public enum ConfigurationMode

### src/FSBS.Domain/Enums/CustomerClass.cs
- L3  [enum]  public enum CustomerClass

### src/FSBS.Domain/Enums/DiscountType.cs
- L3  [enum]  public enum DiscountType

### src/FSBS.Domain/Enums/EnrolmentStatus.cs
- L3  [enum]  public enum EnrolmentStatus

### src/FSBS.Domain/Enums/InvitationStatus.cs
- L3  [enum]  public enum InvitationStatus

### src/FSBS.Domain/Enums/InviteeRole.cs
- L3  [enum]  public enum InviteeRole

### src/FSBS.Domain/Enums/InvoiceStatus.cs
- L3  [enum]  public enum InvoiceStatus

### src/FSBS.Domain/Enums/OrgRole.cs
- L3  [enum]  public enum OrgRole

### src/FSBS.Domain/Enums/PaymentMethod.cs
- L3  [enum]  public enum PaymentMethod

### src/FSBS.Domain/Enums/PaymentStatus.cs
- L3  [enum]  public enum PaymentStatus

### src/FSBS.Domain/Enums/ReportRunStatus.cs
- L3  [enum]  public enum ReportRunStatus

### src/FSBS.Domain/Enums/SlotStatus.cs
- L3  [enum]  public enum SlotStatus

### src/FSBS.Domain/Enums/TrainingType.cs
- L3  [enum]  public enum TrainingType

### src/FSBS.Domain/Events/BookingApprovedEvent.cs
- L12  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Events/BookingCancelledEvent.cs
- L15  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Events/BookingConfirmedEvent.cs
- L16  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Events/BookingRejectedEvent.cs
- L13  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Events/InvitationClaimedEvent.cs
- L15  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Events/InvitationIssuedEvent.cs
- L21  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Events/SlotBookedEvent.cs
- L21  [property]  public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

### src/FSBS.Domain/Exceptions/DomainException.cs
- L3  [class]  public abstract class DomainException(string message) : Exception(message);

### src/FSBS.Domain/Interfaces/IAircraftTypeRepository.cs
- L5  [interface]  public interface IAircraftTypeRepository

### src/FSBS.Domain/Interfaces/IReconfigurationSlotRepository.cs
- L5  [interface]  public interface IReconfigurationSlotRepository

### src/FSBS.Domain/ValueObjects/DateTimeRange.cs
- L9  [property]  public DateTimeOffset Start { get; }
- L10  [property]  public DateTimeOffset End { get; }
- L12  [method]  public DateTimeRange(DateTimeOffset start, DateTimeOffset end)
- L21  [property]  public TimeSpan Duration => End - Start;
- L22  [property]  public int DurationMins => (int)Duration.TotalMinutes;

### src/FSBS.Domain/ValueObjects/IdempotencyKey.cs
- L10  [method]  public static IdempotencyKey New() => new(Guid.NewGuid());
- L12  [method]  public static implicit operator Guid(IdempotencyKey key) => key.Value;
- L13  [method]  public static implicit operator IdempotencyKey(Guid value) => new(value);
- L15  [method]  public override string ToString() => Value.ToString();

### src/FSBS.Infrastructure.Persistence.Repositories.Interfaces/IInvitationRepository.cs
- L5  [interface]  public interface IInvitationRepository

### src/FSBS.Infrastructure.Persistence.Repositories/AircraftTypeRepository.cs
- L10  [method]  public async Task<IReadOnlyList<AircraftType>> ListAllAsync(CancellationToken ct = default) =>
- L15  [method]  public Task<AircraftType?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
- L18  [method]  public async Task AddAsync(AircraftType aircraftType, CancellationToken ct = default) =>

### src/FSBS.Infrastructure.Persistence.Repositories/AvailabilityReadService.cs
- L16  [method]  public async Task<AvailabilityGridDto> GetAvailabilityAsync(

### src/FSBS.Infrastructure.Persistence.Repositories/BookingRepository.cs
- L17  [method]  public async Task<PagedResult<BookingSummaryDto>> GetMyBookingsPageAsync(
- L92  [method]  public async Task<IReadOnlyList<BookingSummaryDto>> GetMyBookingsForRangeAsync(
- L146  [method]  public async Task<BookingDetailDto?> GetMyBookingDetailAsync(
- L209  [method]  public async Task<IReadOnlyList<BookingSummaryDto>> GetPendingApprovalAsync(CancellationToken ct)

### src/FSBS.Infrastructure.Persistence.Repositories/BookingWriteRepository.cs
- L17  [method]  public Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
- L23  [method]  public Task<Booking?> FindByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default) =>
- L27  [method]  public async Task<IReadOnlyList<BookingSlot>> FindConflictingSlotsAsync(
- L39  [method]  public Task<BookingSlot?> FindNextSlotOnBayAsync(
- L53  [method]  public Task<BookingSlot?> FindPrecedingSlotOnBayAsync(
- L67  [method]  public Task AddAsync(Booking booking, CancellationToken ct = default)

### src/FSBS.Infrastructure.Persistence.Repositories/InstructorRepository.cs
- L10  [method]  public Task<Instructor?> FindByUserIdAsync(Guid userId, CancellationToken ct = default) =>
- L15  [method]  public async Task<IReadOnlyList<Instructor>> ListRatedForAsync(
- L24  [method]  public async Task<IReadOnlyList<Instructor>> ListAllAsync(CancellationToken ct = default) =>

### src/FSBS.Infrastructure.Persistence.Repositories/InstructorScheduleRepository.cs
- L9  [class]  public sealed class InstructorScheduleRepository(FsbsDbContext db) : IInstructorScheduleRepository
- L11  [method]  public Task<Guid?> GetInstructorIdForUserAsync(Guid userId, CancellationToken ct = default) =>
- L17  [method]  public Task<InstructorWeeklyPattern?> GetActivePatternAsync(Guid instructorId, CancellationToken ct = default) =>
- L23  [method]  public async Task<InstructorWeeklyPattern> ReplaceActivePatternAsync(
- L77  [method]  public async Task<IReadOnlyList<InstructorAvailability>> GetOverridesAsync(
- L87  [method]  public async Task<InstructorAvailability> AddOverrideAsync(
- L108  [method]  public async Task<InstructorAvailability> UpdateOverrideAsync(
- L129  [method]  public async Task DeleteOverrideAsync(Guid instructorId, Guid overrideId, CancellationToken ct = default)
- L140  [method]  public async Task ReplaceDayAvailableOverridesAsync(

### src/FSBS.Infrastructure.Persistence.Repositories/InvitationRepository.cs
- L34  [method]  public Task<Invitation?> FindPendingByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
- L46  [method]  public async Task ClaimWithNewUserAsync(

### src/FSBS.Infrastructure.Persistence.Repositories/OrganisationAccountRepository.cs
- L11  [class]  public sealed class OrganisationAccountRepository(FsbsDbContext db) : IOrganisationAccountRepository
- L13  [method]  public async Task<OrgAccountDto?> GetAccountAsync(Guid orgId, CancellationToken ct = default)
- L23  [method]  public async Task<PaymentDto> RecordPaymentAsync(
- L57  [method]  public async Task<PaymentDto> VerifyPaymentAsync(
- L71  [method]  public async Task<PaymentDto> VoidPaymentAsync(

### src/FSBS.Infrastructure.Persistence.Repositories/PricingPolicyRepository.cs
- L11  [method]  public Task<PricingPolicy?> FindApplicableAsync(
- L29  [method]  public async Task<IReadOnlyList<DiscountRule>> GetDiscountRulesAsync(

### src/FSBS.Infrastructure.Persistence.Repositories/ReconfigurationSlotRepository.cs
- L10  [method]  public Task<ReconfigurationSlot?> FindByPrecedingBookingAsync(
- L16  [method]  public Task<bool> HasOverlapAsync(
- L28  [method]  public async Task AddAsync(ReconfigurationSlot slot, CancellationToken ct = default)
- L34  [method]  public void Remove(ReconfigurationSlot slot) =>

### src/FSBS.Infrastructure.Persistence.Repositories/ReconfigurationTemplateRepository.cs
- L10  [method]  public Task<ReconfigurationTemplate?> FindAsync(

### src/FSBS.Infrastructure.Persistence.Repositories/ReferenceDataRepository.cs
- L9  [class]  public sealed class ReferenceDataRepository(FsbsDbContext db) : IReferenceDataRepository
- L11  [method]  public Task<IReadOnlyList<ReferenceItemDto>> GetCustomerClassesAsync(CancellationToken ct = default) =>
- L15  [method]  public Task<IReadOnlyList<ReferenceItemDto>> GetDiscountTypesAsync(CancellationToken ct = default) =>
- L19  [method]  public Task<IReadOnlyList<ReferenceItemDto>> GetPaymentMethodsAsync(CancellationToken ct = default) =>
- L23  [method]  public async Task<IReadOnlyList<AccountStatusDto>> GetAccountStatusesAsync(CancellationToken ct = default) =>
- L28  [method]  public async Task<ReferenceItemDto> UpsertCustomerClassAsync(UpsertReferenceItemRequest r, CancellationToken ct = default)
- L37  [method]  public async Task<ReferenceItemDto> UpsertDiscountTypeAsync(UpsertReferenceItemRequest r, CancellationToken ct = default)
- L46  [method]  public async Task<ReferenceItemDto> UpsertPaymentMethodAsync(UpsertReferenceItemRequest r, CancellationToken ct = default)
- L55  [method]  public async Task<AccountStatusDto> UpsertAccountStatusAsync(UpsertAccountStatusRequest r, CancellationToken ct = default)

### src/FSBS.Infrastructure.Persistence.Repositories/SimulatorRepository.cs
- L11  [method]  public Task<SimulatorUnit?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
- L19  [method]  public Task<SimulatorBay?> FindBayAsync(Guid bayId, CancellationToken ct = default) =>
- L25  [method]  public Task<SimulatorConfiguration?> FindConfigurationAsync(Guid configurationId, CancellationToken ct = default) =>
- L29  [method]  public async Task<IReadOnlyList<SimulatorConfiguration>> ListConfigurationsForTrainingTypeAsync(
- L37  [method]  public async Task<IReadOnlyList<SimulatorUnit>> ListAllAsync(CancellationToken ct = default) =>
- L46  [method]  public Task AddUnitAsync(SimulatorUnit unit, CancellationToken ct = default) =>
- L49  [method]  public Task AddBayAsync(SimulatorBay bay, CancellationToken ct = default) =>
- L52  [method]  public Task AddConfigurationAsync(SimulatorConfiguration configuration, CancellationToken ct = default) =>

### src/FSBS.Infrastructure.Persistence.Repositories/UserProfileRepository.cs
- L8  [class]  public sealed class UserProfileRepository(FsbsDbContext db) : IUserProfileRepository
- L10  [method]  public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
- L13  [method]  public async Task UpsertAsync(UserProfile profile, CancellationToken ct = default)

### src/FSBS.Shared/Bookings/BookingDetailDto.cs
- L3  [record]  public record BookingDetailDto(
- L25  [record]  public record BookingSlotDto(
- L36  [record]  public record BookingApprovalDto(
- L42  [record]  public record BookingDiscountDto(

### src/FSBS.Shared/Bookings/BookingSummaryDto.cs
- L3  [record]  public record BookingSummaryDto(

### src/FSBS.Shared/Class1.cs
- L3  [class]  public class Class1   *(recommend delete — `dotnet new` leftover)*

### src/FSBS.Shared/Common/PagedResult.cs
- L3  [record]  public record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor);

### src/FSBS.Shared/Payments/PaymentDtos.cs
- L3  [record]  public record RecordPaymentRequest(
- L10  [record]  public record PaymentDto(
- L21  [record]  public record OrgAccountDto(

### src/FSBS.Shared/ReferenceData/ReferenceDataDtos.cs
- L3  [record]  public sealed record ReferenceItemDto(string Code, string Label, bool IsActive);
- L5  [record]  public sealed record AccountStatusDto(string Code, string Label, bool IsActive, bool AllowsBooking);
- L7  [record]  public sealed record UpsertReferenceItemRequest(string Code, string Label, bool IsActive);
- L9  [record]  public sealed record UpsertAccountStatusRequest(string Code, string Label, bool IsActive, bool AllowsBooking);

### src/FSBS.Shared/Simulators/AircraftTypeDto.cs
- L3  [record]  public record AircraftTypeDto(

### src/FSBS.Shared/Simulators/SimulatorDetailDto.cs
- L3  [record]  public record SimulatorDetailDto(
- L14  [record]  public record SimulatorBayDto(
- L19  [record]  public record SimulatorConfigurationDto(

### src/FSBS.Shared/Simulators/SimulatorRequestDtos.cs
- L5  [record]  public record CreateSimulatorUnitRequest(
- L12  [record]  public record UpdateSimulatorUnitRequest(
- L22  [record]  public record CreateSimulatorBayRequest(
- L26  [record]  public record UpdateSimulatorBayRequest(
- L33  [record]  public record CreateSimulatorConfigurationRequest(
- L41  [record]  public record UpdateSimulatorConfigurationRequest(

### src/FSBS.Shared/UserProfiles/UserProfileDto.cs
- L3  [record]  public sealed record UserProfileDto(
- L13  [record]  public sealed record UpdateUserProfileRequest(
- L22  [record]  public sealed record PhotoUploadUrlResponse(string UploadUrl, string ObjectKey);

## Verification

This audit is descriptive — no code changes proposed. To re-run after fixing docs, regenerate the list with the same detector:

```bash
# Build the awk detector (one-time)
cat > /tmp/find_undoc.awk << 'AWK'
# (See implementation block earlier in this session, or reconstruct from
#  the rules in the "Detector caveats" section.)
AWK

find src/FSBS.Domain src/FSBS.Application src/FSBS.Shared \
     src/FSBS.Api src/FSBS.Infrastructure.Persistence.Repositories \
     src/FSBS.Infrastructure.Persistence.Repositories.Interfaces \
     -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" \
  | sort | xargs awk -f /tmp/find_undoc.awk
```

A clean run prints zero lines.

For ongoing enforcement, consider enabling `<DocumentationFile>` + `WarningsAsErrors>CS1591</WarningsAsErrors>` on the projects that should be 100% documented (e.g. `FSBS.Domain`, `FSBS.Shared`). That replaces this audit with a build-time gate.
