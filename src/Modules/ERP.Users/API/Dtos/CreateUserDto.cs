using FluentValidation;

namespace ERP.Users.API.Dtos;

public record CreateUserDto(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Department,
    string? JobTitle
);

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must have at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must have at least one digit.");
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName != null);
    }
}
