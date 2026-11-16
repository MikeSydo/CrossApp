using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class SaveOrderLineRequestValidator : AbstractValidator<SaveOrderLineRequest>
{
    public SaveOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ідентифікатор товару є обов'язковим.")
            .MaximumLength(50).WithMessage("Ідентифікатор товару не може перевищувати 50 символів.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва товару є обов'язковою.")
            .MaximumLength(200).WithMessage("Назва товару не може перевищувати 200 символів.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Ціна товару не може бути від'ємною.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Кількість товару повинна бути більшою за нуль.");
    }
}

public sealed class SaveOrderRequestValidator : AbstractValidator<SaveOrderRequest>
{
    public SaveOrderRequestValidator()
    {
        RuleFor(x => x.EffectiveCustomer)
            .NotEmpty().WithMessage("Ідентифікатор/ім'я клієнта є обов'язковим.")
            .MaximumLength(100).WithMessage("Ідентифікатор/ім'я клієнта не може перевищувати 100 символів.");

        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Список товарів (lines) не може бути порожнім.")
            .Must(lines => lines is { Count: > 0 }).WithMessage("Список товарів (lines) не може бути порожнім.");

        When(x => x.Lines is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Lines).SetValidator(new SaveOrderLineRequestValidator());
        });
    }
}