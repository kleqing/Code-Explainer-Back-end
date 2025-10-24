using CodeExplainer.BusinessObject;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;
using CodeExplainer.Services.Interfaces;
using MaIN.Core.Hub;
using Microsoft.EntityFrameworkCore;

namespace CodeExplainer.Services.Implements;

public class ChatServices : IChatServices
{
    private readonly ApplicationDbContext _context;
    
    public ChatServices(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ChatSendResponse> SendMessageAsync(Guid userId, ChatSendRequest request)
    {
        ChatSession chatSession;
        if (request.ChatSessionId == Guid.Empty)
        {
            chatSession = new ChatSession
            {
                ChatSessionId = Guid.NewGuid(),
                UserId = userId,
                Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.ChatSessions.Add(chatSession);
        }
        else
        {
            chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(x => x.ChatSessionId == request.ChatSessionId && x.UserId == userId)
                ?? new ChatSession
                {
                    ChatSessionId = request.ChatSessionId,
                    UserId = userId,
                    Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            if (_context.Entry(chatSession).State == EntityState.Detached)
                _context.ChatSessions.Add(chatSession);
            chatSession.UpdatedAt = DateTime.Now;
        }

        var message = new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            ChatSessionId = chatSession.ChatSessionId,
            Role = "user",
            Content = $"{request.Message}\n\n```{request.Language}\n{request.SourceCode}\n```",
            CreatedAt = DateTime.Now
        };
        _context.ChatMessages.Add(message);

        var prompt = $"Explain what this {request.Language} code does. Also suggest improvements if possible:\n\n{request.SourceCode}";

        var aiResponse = await AIHub.Chat()
            .WithModel("gemini-2.0-flash")
            .WithMessage(prompt)
            .CompleteAsync();

        var replyText = aiResponse.Message.Content;

        var replyMessage = new ChatMessage
        {
            ChatMessageId = Guid.NewGuid(),
            ChatSessionId = chatSession.ChatSessionId,
            Role = "assistant",
            Content = replyText,
            CreatedAt = DateTime.Now
        };
        _context.ChatMessages.Add(replyMessage);

        await _context.SaveChangesAsync();

        return new ChatSendResponse
        {
            ChatSessionId = chatSession.ChatSessionId,
            UserMessage = message.Content,
            AIResponse = replyMessage.Content
        };
    }

    public async Task<List<ChatSession>> GetUserSessionsAsync(Guid userId)
        => await _context.ChatSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid chatSessionId)
        => await _context.ChatMessages
            .Where(x => x.ChatSessionId == chatSessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
}