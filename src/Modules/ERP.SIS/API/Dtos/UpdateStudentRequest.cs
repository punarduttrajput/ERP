namespace ERP.SIS.API.Dtos;

public record UpdateStudentRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    string MobileNumber,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? PermanentAddress,
    string? CurrentAddress,
    string Category
);
