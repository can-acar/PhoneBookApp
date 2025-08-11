using ContactService.ApiContract.Request.Commands;
using FluentValidation;

namespace ContactService.ApiContract.Validations;

public class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Kişi adı boş olamaz")
            .MinimumLength(2)
            .WithMessage("Kişi adı en az 2 karakter olmalıdır")
            .MaximumLength(100)
            .WithMessage("Kişi adı en fazla 100 karakter olabilir")
            .Matches(@"^[a-zA-ZğüşöçıĞÜŞÖÇİ\s]+$")
            .WithMessage("Kişi adı sadece harf ve boşluk karakteri içermelidir");

        RuleFor(x => x.Company)
            .MaximumLength(200)
            .WithMessage("Şirket adı en fazla 200 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Company));

        RuleFor(x => x.ContactInfos)
            .NotNull()
            .WithMessage("İletişim bilgileri boş olamaz");

        RuleForEach(x => x.ContactInfos)
            .SetValidator(new CreateContactInfoDtoValidator());
    }
}