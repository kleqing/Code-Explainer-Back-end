using CodeExplainer.BusinessObject.Models;

namespace CodeExplainer.Services.Interfaces;

public interface IAuthTokenProcess
{
    (string Token, DateTime Expiry) GenerateToken(User user);
    string GenerateRefreshToken();
    void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiry);
    void DeleteAuthTokenCookie(string key);
    Task<string> GenerateEmailConfirmationTokenAsync(User user);
    Task<string> GeneratePasswordTokenResetAsync(User user);
    bool ValidateEmailConfirmationToken(User user, string token);
}