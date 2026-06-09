using FluentValidation;

namespace ERP.Auth.API.Dtos;

public record RefreshTokenRequest(string Token);

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
