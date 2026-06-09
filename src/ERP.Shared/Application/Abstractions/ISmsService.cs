namespace ERP.Shared.Application.Abstractions;

public interface ISmsService
{
    Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default);
}
