namespace CodeExplainer.BusinessObject.Request;

public class ChatSendRequest
{
    public Guid ChatSessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
}