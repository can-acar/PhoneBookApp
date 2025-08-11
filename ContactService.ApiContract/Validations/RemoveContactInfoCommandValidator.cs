using ContactService.ApiContract.Request.Commands;
using FluentValidation;

namespace ContactService.ApiContract.Validations;

public class RemoveContactInfoCommandValidator : AbstractValidator<RemoveContactInfoCommand>
{
    public RemoveContactInfoCommandValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .WithMessage("Kişi ID'si boş olamaz");

        RuleFor(x => x.ContactInfoId)
            .NotEmpty()
            .WithMessage("İletişim bilgisi ID'si boş olamaz");
    }
}
