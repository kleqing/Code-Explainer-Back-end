using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.Services.Interfaces;
using CodeExplainer.Shared.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeExplainer.Services.Implements;

public class AuthTokenProcess : IAuthTokenProcess
{
    private readonly Jwt _jwt;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _env;
    
    public AuthTokenProcess(IOptions<Jwt> jwt, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
    {
        _jwt = jwt.Value;
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }
    
    public (string Token, DateTime Expiry) GenerateToken(User user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), //* Set the NameIdentifier claim to the user's ID
            new Claim(ClaimTypes.Role, user.UserRole.ToString()) //* Add user role as a claim
        };

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryInMinutes);

        var token = new JwtSecurityToken(issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

        return (jwtToken, expires);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiry)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, token, BuildCookieOptions(expiry));
    }

    public void DeleteAuthTokenCookie(string key)
    {
        var context = _httpContextAccessor.HttpContext;
        context?.Response.Cookies.Delete(key);
    }
    
    public Task<string> GenerateEmailConfirmationTokenAsync(User user)
        => Task.FromResult(GenerateTokenWithPurpose(user, "email_confirmation", TimeSpan.FromHours(24)));
    
    public Task<string> GeneratePasswordTokenResetAsync(User user)
    {
        var token = GenerateRefreshToken();
        return Task.FromResult(token);
    }

    private string GenerateTokenWithPurpose(User user, string purpose, TimeSpan lifetime)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("purpose", purpose)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public bool ValidateEmailConfirmationToken(User user, string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwt.Secret);

        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for token expiration
            };

            var principal = tokenHandler.ValidateToken(token, parameters, out _);
            var purpose = principal.FindFirst("purpose")?.Value;
            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                         principal.FindFirst(ClaimTypes.NameIdentifier)?.Value; //* Fallback to NameIdentifier if Sub is not found

            return purpose == "email_confirmation" && userId == user.UserId.ToString();
        }
        catch
        {
            return false;
        }
    }
    
    private CookieOptions BuildCookieOptions(DateTime? expiry = null)
    {
        return new CookieOptions
        {
            Expires = expiry,
            HttpOnly = true,
            IsEssential = true,
            Secure = !_env.IsDevelopment(), //* Set to true in production
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None, //* Set Lax if both frontend and backend are on the same domain
            // Path = "/" //* Uncomment this line if you want the cookie to be accessible across all paths
        };
    }
}