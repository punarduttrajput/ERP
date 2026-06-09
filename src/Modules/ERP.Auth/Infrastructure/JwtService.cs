using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Auth.Infrastructure;

public sealed class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public JwtService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is required.");
        _issuer = configuration["Jwt:Issuer"] ?? "erp-platform";
        _audience = configuration["Jwt:Audience"] ?? "erp-clients";
        _accessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var mins) ? mins : 15;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_key);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tid", user.TenantId.ToString()),
            new("name", user.FullName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string token, string familyId) GenerateRefreshToken(string? existingFamilyId = null)
    {
        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);
        var familyId = existingFamilyId ?? Guid.NewGuid().ToString();
        return (token, familyId);
    }

    public string GenerateMfaChallengeToken(Guid userId, Guid tenantId)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_key);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tid", tenantId.ToString()),
            new Claim("mfa_pending", "true")
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool isValid, Guid userId, Guid tenantId) ValidateMfaChallengeToken(string token)
    {
        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(_key);
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.Zero
            }, out _);

            var mfaPending = principal.FindFirstValue("mfa_pending");
            if (mfaPending != "true")
                return (false, Guid.Empty, Guid.Empty);

            var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var tenantIdStr = principal.FindFirstValue("tid");

            if (!Guid.TryParse(userIdStr, out var userId) || !Guid.TryParse(tenantIdStr, out var tenantId))
                return (false, Guid.Empty, Guid.Empty);

            return (true, userId, tenantId);
        }
        catch
        {
            return (false, Guid.Empty, Guid.Empty);
        }
    }
}
