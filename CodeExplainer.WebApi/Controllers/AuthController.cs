using System.Security.Claims;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;
using CodeExplainer.Repository.Interfaces;
using CodeExplainer.Services.Interfaces;
using CodeExplainer.Shared.Exceptions;
using CodeExplainer.Shared.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeExplainer.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthTokenProcess _authTokenProcess;
    private readonly IAuthorizeServices _authorizeServices;
    private readonly IConfiguration _configuration;
    
    public AuthController(IUserRepository userRepository, IAuthTokenProcess authTokenProcess, IAuthorizeServices authorizeServices, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _authTokenProcess = authTokenProcess;
        _authorizeServices = authorizeServices;
        _configuration = configuration;
    }
    
    [AllowAnonymous]
    [HttpGet("login-google")]
    public IActionResult LoginWithGoogle(string returnUrl)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
    
    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return BadRequest("Failed to authenticate with Google.");
        }

        var claimsPrincipal = result.Principal;
        await _authorizeServices.LoginWithGoogle(claimsPrincipal);

        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var name = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value + " " +
                   claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value;
        var avatar = claimsPrincipal.FindFirst("picture")?.Value ?? string.Empty;

        var frontendUrl =
            $"{returnUrl}?email={Uri.EscapeDataString(email)}&name={Uri.EscapeDataString(name)}&avatar={Uri.EscapeDataString(avatar)}";
        return Redirect(frontendUrl);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = new BaseResultResponse<User>();

        try
        {
            var user = await _authorizeServices.CreateAccount(request);
            response.StatusCode = StatusCodes.Status201Created;
            response.Success = true;
            response.Message = "Account created successfully. Please check your email to confirm your account.";
            response.Data = user;
        }
        catch (Exception e)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
            response.Errors = new List<string> { e.Message };
            response.Data = null;
        }
        return Ok(response);
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = new BaseResultResponse<LoginResponse>();

        try
        {
            var user = await _authorizeServices.Login(request);
            response.StatusCode = StatusCodes.Status200OK;
            response.Success = true;
            response.Message = "Login successful.";
            response.Data = user;
        }
        catch (Exception e)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
            response.Errors = new List<string> { e.Message };
            response.Data = null;
        }
        return Ok(response);
    }
    
    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest("Invalid user ID.");
        }

        var isValid = _authTokenProcess.ValidateEmailConfirmationToken(user, token);
        if (!isValid)
        {
            return BadRequest("Email confirmation failed.");
        }
        
        var confirmedUser = await _userRepository.ConfirmEmailAsync(user);
        if (confirmedUser == null)
        {
            return BadRequest("Email confirmation failed.");
        }

        //* If verification is successful, redirect to frontend with success message. You may change the URL as needed.
        var frontendUrl = UrlHelper.GetFrontendUrl(_configuration);
        return Redirect($"{frontendUrl}/verify-success?verifiedEmail={Uri.EscapeDataString(user.Email)}");
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var response = new BaseResultResponse<string>();
        
        try
        {
            await _authorizeServices.InitiatePasswordReset(email);
            response.StatusCode = StatusCodes.Status200OK;
            response.Success = true;
            response.Message = "If an account with that email exists, a password reset link has been sent.";
            response.Data = null;
        }
        catch (Exception e)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
            response.Errors = new List<string> { e.Message };
            response.Data = null;
        }
        return Ok(response);
    }

    [HttpGet("reset-password/verify")]
    public async Task<IActionResult> VerifyResetToken([FromQuery] string token)
    {
        var response = new BaseResultResponse<bool>();
        
        if (string.IsNullOrEmpty(token))
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            response.Success = false;
            response.Message = "Token is required.";
            response.Data = false;
            return BadRequest(response);
        }
        
        var isTokenValid = await _authorizeServices.VerifyPasswordResetToken(token);
        if (!isTokenValid)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            response.Success = false;
            response.Message = "Invalid or expired token.";
            response.Data = false;
            return BadRequest(response);
        }
        response.StatusCode = StatusCodes.Status200OK;
        response.Success = true;
        response.Message = "Token is valid.";
        response.Data = true;
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var response = new BaseResultResponse<string>();

        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest("Token is required.");
        }
        
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        try
        {
            await _authorizeServices.ResetPasswordAsync(request);
            response.StatusCode = StatusCodes.Status200OK;
            response.Success = true;
            response.Message = "Password has been reset successfully.";
            response.Data = null;
            return Ok(response);
        }
        catch (GlobalException e)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            response.Success = false;
            response.Message = e.Message;
            response.Errors = new List<string> { e.Message };
            response.Data = null;
            return BadRequest(response);
        }
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendEmailConfirmation(string email)
    {
        var response = new BaseResultResponse<string>();

        try
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Success = false;
                response.Message = "User not found.";
                response.Data = null;
                return NotFound(response);
            }

            await _authorizeServices.ResendEmailConfirmationAsync(user);
            response.StatusCode = StatusCodes.Status200OK;
            response.Success = true;
            response.Message = "Confirmation email resent successfully.";
            response.Data = null;
            return Ok(response);
        }
        catch (Exception e)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
            response.Errors = new List<string> { e.Message };
            response.Data = null;
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var response = new BaseResultResponse<string>();

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Success = false;
                response.Message = "User not found in token.";
                response.Data = null;
                return BadRequest(response);
            }
            
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Success = false;
                response.Message = "User not found.";
                response.Data = null;
                return NotFound(response);
            }
            
            await _authorizeServices.Logout(user);
            response.StatusCode = StatusCodes.Status200OK;
            response.Success = true;
            response.Message = "Logout successful.";
            response.Data = null;
            return Ok(response);
        }
        catch (Exception e)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            response.Success = false;
            response.Message = "An error occurred while processing your request.";
            response.Errors = new List<string> { e.Message };
            response.Data = null;
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }
}