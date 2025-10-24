using System.Security.Claims;
using CodeExplainer.BusinessObject.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeExplainer.Services.Interfaces;
namespace CodeExplainer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationServices _notificationService;
    
    public NotificationController(INotificationServices notificationService)
    {
        _notificationService = notificationService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUserNotifications()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] NotificationCreateRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notification = await _notificationService.CreateNotificationAsync(userId, request.Title, request.Message);
        return Ok(notification);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Ok();
    }
}