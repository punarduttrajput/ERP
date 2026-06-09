namespace ERP.Auth.API.Dtos;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string Name,
    IEnumerable<string> Roles,
    bool MfaRequired = false,
    string? MfaChallengeToken = null
);
