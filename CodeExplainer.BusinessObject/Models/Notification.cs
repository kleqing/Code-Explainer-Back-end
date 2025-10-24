using System.ComponentModel.DataAnnotations;

namespace CodeExplainer.BusinessObject.Models;

public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}