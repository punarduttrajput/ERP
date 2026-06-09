namespace ERP.Tenants.API.Dtos;

public record TenantResponseDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain,
    string Status,
    string? ContactEmail,
    string? Plan,
    DateTime CreatedAt
);
