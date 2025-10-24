namespace CodeExplainer.BusinessObject.Response;

public class UserUpdateProfileResponse
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
}