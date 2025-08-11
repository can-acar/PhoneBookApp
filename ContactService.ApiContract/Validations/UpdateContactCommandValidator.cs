using ContactService.ApiContract.Request.Commands;
using FluentValidation;

namespace ContactService.ApiContract.Validations;

public class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Kişi ID'si boş olamaz");

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
            .NotEmpty()
            .WithMessage("Şirket adı boş olamaz")
            .NotNull().WithMessage("Şirket adı boş olamaz")
            .MinimumLength(2)
            .WithMessage("Şirket adı en az 2 karakter olmalıdır")
            .MaximumLength(200)
            .WithMessage("Şirket adı en fazla 100 karakter olabilir")
            .Matches(@"^[a-zA-ZğüşöçıĞÜŞÖÇİ\s]+$")
            .WithMessage("Şirket adı sadece harf ve boşluk karakteri içermelidir");

        RuleFor(x => x.ContactInfos)
            .NotNull()
            .WithMessage("İletişim bilgileri boş olamaz");

        RuleForEach(x => x.ContactInfos)
            .SetValidator(new UpdateContactInfoDtoValidator());
    }
}