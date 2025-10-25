using Microsoft.AspNetCore.Http;

namespace CodeExplainer.BusinessObject.Request;

public class UserProfileUpdateRequest
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public IFormFile? ProfilePictureUrl { get; set; }
}