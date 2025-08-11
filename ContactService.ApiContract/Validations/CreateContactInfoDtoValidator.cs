using ContactService.ApiContract.Contracts;
using ContactService.Domain.Enums;
using FluentValidation;

namespace ContactService.ApiContract.Validations;

public class CreateContactInfoDtoValidator : AbstractValidator<CreateContactInfoDto>
{
    public CreateContactInfoDtoValidator()
    {
        RuleFor(x => x.InfoType)
            .Must(x => Enum.IsDefined(typeof(ContactInfoType), x))
            .WithMessage("Geçersiz iletişim türü");

        RuleFor(x => x.InfoValue)
            .NotEmpty()
            .WithMessage("İletişim içeriği boş olamaz")
            .MaximumLength(500)
            .WithMessage("İletişim içeriği en fazla 500 karakter olabilir");

        // Telefon numarası validation
        RuleFor(x => x.InfoValue)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Geçersiz telefon numarası formatı")
            .When(x => x.InfoType == (int)ContactInfoType.PhoneNumber);

        // Email validation
        RuleFor(x => x.InfoValue)
            .EmailAddress()
            .WithMessage("Geçersiz email formatı")
            .When(x => x.InfoType == (int)ContactInfoType.EmailAddress);

        // Lokasyon validation
        RuleFor(x => x.InfoValue)
            .MinimumLength(2)
            .WithMessage("Lokasyon en az 2 karakter olmalıdır")
            .MaximumLength(100)
            .WithMessage("Lokasyon en fazla 100 karakter olabilir")
            .When(x => x.InfoType == (int)ContactInfoType.Location);
    }
}