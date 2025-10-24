using System.Security.Claims;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;

namespace CodeExplainer.Services.Interfaces;

public interface IAuthorizeServices
{
    Task<User> LoginWithGoogle(ClaimsPrincipal claimsPrincipal);
    Task<User?> CreateAccount(RegisterRequest request);
    Task<LoginResponse?> Login(LoginRequest request);
    Task InitiatePasswordReset(string email);
    Task<bool> VerifyPasswordResetToken(string token);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ResendEmailConfirmationAsync(User user);
    Task Logout(User user);
}