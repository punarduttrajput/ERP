using ERP.Auth.Domain;

namespace ERP.Auth.Application.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    (string token, string familyId) GenerateRefreshToken(string? existingFamilyId = null);
    string GenerateMfaChallengeToken(Guid userId, Guid tenantId);
    (bool isValid, Guid userId, Guid tenantId) ValidateMfaChallengeToken(string token);
}
