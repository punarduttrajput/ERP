namespace ERP.Shared.Application.Abstractions;

public interface IWhatsAppService
{
    Task SendOtpAsync(string toMobileNumber, string otp, CancellationToken cancellationToken = default);
}
