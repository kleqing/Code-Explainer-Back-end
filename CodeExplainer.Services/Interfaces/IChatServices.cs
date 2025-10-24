using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;

namespace CodeExplainer.Services.Interfaces;

public interface IChatServices
{
    Task<ChatSendResponse> SendMessageAsync(Guid userId, ChatSendRequest request);
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId);
    Task<List<ChatMessage>> GetMessagesAsync(Guid chatSessionId);
}