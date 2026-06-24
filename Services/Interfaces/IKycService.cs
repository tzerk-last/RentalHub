namespace RentalHub.Services.Interfaces;

public interface IKycService
{
    Task<(bool approved, string reason)> VerifyAsync(string documentPath, string selfiePath);
}
