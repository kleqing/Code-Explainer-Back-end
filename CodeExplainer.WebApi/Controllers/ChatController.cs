using System.Security.Claims;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeExplainer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatServices _chatService;

    public ChatController(IChatServices chatService)
    {
        _chatService = chatService;
    }
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatSendRequest request)
    {
        var userId = GetUserId();
        var result = await _chatService.SendMessageAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetUserId();
        var sessions = await _chatService.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    [HttpGet("messages/{sessionId:guid}")]
    public async Task<IActionResult> GetMessages(Guid sessionId)
    {
        var messages = await _chatService.GetMessagesAsync(sessionId);
        return Ok(messages);
    }
}