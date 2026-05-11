using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Payments;
using FluentValidation;

namespace FSBS.Application.Organisations.Commands;

public record VerifyOrganisationPaymentCommand(Guid OrgId, Guid PaymentId)
    : ICommand<PaymentDto>;

public record VoidOrganisationPaymentCommand(Guid OrgId, Guid PaymentId, string Reason)
    : ICommand<PaymentDto>;

public sealed class VerifyOrganisationPaymentCommandValidator
    : AbstractValidator<VerifyOrganisationPaymentCommand>
{
    public VerifyOrganisationPaymentCommandValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}

public sealed class VoidOrganisationPaymentCommandValidator
    : AbstractValidator<VoidOrganisationPaymentCommand>
{
    public VoidOrganisationPaymentCommandValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10)
            .WithMessage("Void reason must be at least 10 characters.");
    }
}
