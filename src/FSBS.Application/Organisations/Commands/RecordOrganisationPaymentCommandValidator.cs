using FluentValidation;

namespace FSBS.Application.Organisations.Commands;

public sealed class RecordOrganisationPaymentCommandValidator : AbstractValidator<RecordOrganisationPaymentCommand>
{
    private static readonly string[] ValidPaymentMethods =
        ["BankTransfer", "Cheque", "Cash", "CreditNote", "Adjustment"];

    public RecordOrganisationPaymentCommandValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.AmountGbp).GreaterThan(0);
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => ValidPaymentMethods.Contains(m))
            .WithMessage("PaymentMethod must be one of: BankTransfer, Cheque, Cash, CreditNote, Adjustment.");
    }
}

