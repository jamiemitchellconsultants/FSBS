using FluentValidation;

namespace FSBS.Application.ReferenceData.Commands;

public sealed class UpsertCustomerClassCommandValidator : AbstractValidator<UpsertCustomerClassCommand>
{
    public UpsertCustomerClassCommandValidator()
    {
        RuleFor(x => x.Item.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Item.Label).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpsertDiscountTypeCommandValidator : AbstractValidator<UpsertDiscountTypeCommand>
{
    public UpsertDiscountTypeCommandValidator()
    {
        RuleFor(x => x.Item.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Item.Label).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpsertPaymentMethodCommandValidator : AbstractValidator<UpsertPaymentMethodCommand>
{
    public UpsertPaymentMethodCommandValidator()
    {
        RuleFor(x => x.Item.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Item.Label).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpsertAccountStatusCommandValidator : AbstractValidator<UpsertAccountStatusCommand>
{
    public UpsertAccountStatusCommandValidator()
    {
        RuleFor(x => x.Item.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Item.Label).NotEmpty().MaximumLength(100);
    }
}
