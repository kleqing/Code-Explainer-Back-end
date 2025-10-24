using System.Net;
using System.Security.Claims;
using CodeExplainer.BusinessObject.Enum;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;
using CodeExplainer.Repository.Interfaces;
using CodeExplainer.Services.Interfaces;
using CodeExplainer.Shared.Exceptions;
using CodeExplainer.Shared.Utils;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace CodeExplainer.Services.Implements;

public class AuthorizeServices
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthTokenProcess _authTokenProcess;
    private readonly IDatabase _redisDatabase;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    private const string RedisPrefix = "reset-password";

    public AuthorizeServices(IUserRepository userRepository, IAuthTokenProcess authTokenProcess, IDatabase redisDatabase, IEmailSender emailSender, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _authTokenProcess = authTokenProcess;
        _redisDatabase = redisDatabase;
        _emailSender = emailSender;
        _configuration = configuration;
    }
    
    public async Task<User> LoginWithGoogle(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal == null)
        {
            throw new GlobalException("ClaimsPrincipal is null");
        }

        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        
        if (string.IsNullOrEmpty(email))
        {
            throw new GlobalException("Email claim is missing");
        }
        
        var user = await _userRepository.FindByEmailAsync(email);
        
        if (user == null)
        {
            // User does not exist, create a new user
            user = new User
            {
                Email = email,
                UserName = email.Split('@')[0],
                EmailConfirmed = true,
                ProfilePictureUrl = claimsPrincipal.FindFirst("picture")?.Value ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _userRepository.CreateAsync(user);
            await _userRepository.AddToRoleAsync(user, UserRole.User);
        }
        
        var (jwtToken, expiry) = _authTokenProcess.GenerateToken(user);
        var refreshToken = _authTokenProcess.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        await _userRepository.UpdateAsync(user);
        
        _authTokenProcess.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expiry);
        _authTokenProcess.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", refreshToken, refreshTokenExpiry);
        
        return user;
    }

    public async Task<User?> CreateAccount(RegisterRequest request)
    {
        var existingUser = await _userRepository.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new GlobalException("Email is already in use");
        }
        
        var existingUserName = await _userRepository.FindByNameAsync(request.UserName);
        if (existingUserName != null)
        {
            throw new GlobalException("Username is already in use");
        }
        
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await _userRepository.CreateAsync(user, request.Password);
        await _userRepository.AddToRoleAsync(user, UserRole.User);
        
        var token = await _authTokenProcess.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(token);
        var backendUrl = UrlHelper.GetBackendUrl(_configuration);

         var confirmationLink =
             $"{backendUrl}/api/auth/confirm-email?userId={user.UserId}&token={encodedToken}";

        await _emailSender.SendEmailAsync(user.Email, "Verify your email", confirmationLink);

        return user;
    }
    
    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new GlobalException("Invalid email or password");
        }

        var isPasswordValid = await _userRepository.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new GlobalException("Invalid email or password");
        }

        if (!await _userRepository.IsEmailConfirmedAsync(user))
        {
            throw new GlobalException("Email is not confirmed");
        }

        var (jwtToken, expiry) = _authTokenProcess.GenerateToken(user);
        var refreshToken = _authTokenProcess.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        await _userRepository.UpdateAsync(user);
        
        _authTokenProcess.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expiry);
        _authTokenProcess.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", refreshToken, refreshTokenExpiry);

        return new LoginResponse
        {   
            RefreshToken = user.RefreshToken,
            RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
        };
    }
    
    public async Task InitiatePasswordReset(string email)
    {
        var user = await _userRepository.FindByEmailAsync(email);
        if (user == null)
        {
            //* To prevent email enumeration, do not reveal that the user does not exist
            return;
        }
        
        if (!await _userRepository.IsEmailConfirmedAsync(user))
        {
            //* If email is not confirmed, resend confirmation email automatically
            await ResendEmailConfirmationAsync(user);
            throw new GlobalException("Your email is not verified. We've resent the verification link.");
        }
        
        var token = await _authTokenProcess.GeneratePasswordTokenResetAsync(user);
        var redisKey = $"{RedisPrefix}:{token}";

        try
        {
            bool result = await _redisDatabase.StringSetAsync(redisKey, user.UserId.ToString(), TimeSpan.FromHours(1), When.NotExists);
            if (result)
            {
                var encodedToken = Uri.EscapeDataString(token);
                var frontendUrl = UrlHelper.GetFrontendUrl(_configuration);

                var resetLink =
                    $"{frontendUrl}/api/auth/reset-password/verify?token={encodedToken}"; 
                //* For testing the token in backend without frontend, replace the frontendUrl with backendUrl
                
                await _emailSender.SendEmailAsync(user.Email, "Reset your password", resetLink);
            }
            else
            {
                // A reset request is already in progress
                throw new GlobalException("A password reset request is already in progress. Please check your email.");
            }
        }
        catch
        {
            throw new GlobalException("An error occurred while processing your request. Please try again later.");
        }
    }
    
    public async Task<bool> VerifyPasswordResetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }
        
        var redisKey = $"{RedisPrefix}:{token}";
        try
        {
            return await _redisDatabase.KeyExistsAsync(redisKey);
        }
        catch (Exception ex)
        {
            throw new GlobalException("Verify Password Reset Token",
                $"Error checking token in Redis: {ex.Message}");
        }
    }
    
    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new GlobalException("Password reset token is required");
        }
        
        var decodedToken = Uri.UnescapeDataString(request.Token);
        var redisKey = $"{RedisPrefix}:{decodedToken}";
        var userIdValue = await _redisDatabase.StringGetAsync(redisKey);

        if (userIdValue.IsNullOrEmpty)
        {
            throw new GlobalException("Invalid or expired password reset token");
        }

        var userId = userIdValue.ToString();
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            throw new GlobalException("Invalid or expired password reset token");
        }

        await _userRepository.ResetPasswordAsync(user, request.NewPassword);

        await _redisDatabase.KeyDeleteAsync(redisKey);
    }

    public async Task ResendEmailConfirmationAsync(User user)
    {
        if (!await _userRepository.IsEmailConfirmedAsync(user))
        {
            var token = await _authTokenProcess.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var backendUrl = UrlHelper.GetBackendUrl(_configuration);

            var confirmationLink =
                $"{backendUrl}/api/auth/confirm-email?userId={user.UserId}&token={encodedToken}";

            await _emailSender.SendEmailAsync(user.Email, "Verify your email", confirmationLink);
        }
        else
        {
            throw new GlobalException("Email is already confirmed");
        }
    }

    public async Task Logout(User user)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userRepository.UpdateAsync(user);
        
        _authTokenProcess.DeleteAuthTokenCookie("ACCESS_TOKEN");
        _authTokenProcess.DeleteAuthTokenCookie("REFRESH_TOKEN");
    }
}