namespace CodeExplainer.BusinessObject.Models;

public class ChatMessage
{
    public Guid ChatMessageId { get; set; }
    public Guid ChatSessionId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;

    public virtual DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ChatSession ChatSession { get; set; } = null!;
}