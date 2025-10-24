using CodeExplainer.BusinessObject.Enum;

namespace CodeExplainer.BusinessObject.Models;

public class User
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public UserRole UserRole { get; set; } = UserRole.User;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}