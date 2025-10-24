namespace CodeExplainer.BusinessObject.Models;

public class CodeRequest
{
    public Guid CodeRequestId { get; set; }
    public Guid UserId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string PromptType { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}