namespace ERP.Users.API.Dtos;

public record UserResponseDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Department,
    string? JobTitle,
    string? AvatarUrl,
    bool IsActive,
    DateTime CreatedAt
);
