using FSBS.Application.Bookings.Services;
using FSBS.Application.Pricing.Services;

namespace FSBS.Application.Tests.Common;

/// <summary>
/// Base class for handler tests. Pre-creates NSubstitute mocks for the
/// repositories and services handlers commonly depend on. Subclasses can
/// reassign <see cref="CurrentUser"/> to switch role/identity per test.
/// </summary>
public abstract class HandlerFixtureBase
{
    protected IBookingRepository BookingRepository { get; } = Substitute.For<IBookingRepository>();
    protected ISimulatorRepository SimulatorRepository { get; } = Substitute.For<ISimulatorRepository>();
    protected IInstructorRepository InstructorRepository { get; } = Substitute.For<IInstructorRepository>();
    protected IInvitationRepository InvitationRepository { get; } = Substitute.For<IInvitationRepository>();
    protected IOrganisationRepository OrganisationRepository { get; } = Substitute.For<IOrganisationRepository>();
    protected IPricingPolicyRepository PricingPolicyRepository { get; } = Substitute.For<IPricingPolicyRepository>();
    protected IReconfigurationSlotRepository ReconfigurationSlotRepository { get; } = Substitute.For<IReconfigurationSlotRepository>();
    protected IReconfigurationTemplateRepository ReconfigurationTemplateRepository { get; } = Substitute.For<IReconfigurationTemplateRepository>();
    protected IReconfigurationService ReconfigurationService { get; } = Substitute.For<IReconfigurationService>();
    protected IPricingService PricingService { get; } = Substitute.For<IPricingService>();
    protected IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    protected ICurrentUser CurrentUser { get; set; } = new FakeCurrentUser();
}
