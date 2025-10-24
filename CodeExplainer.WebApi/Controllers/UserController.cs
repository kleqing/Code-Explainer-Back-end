using CodeExplainer.BusinessObject.Request;
using CodeExplainer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeExplainer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserServices _userService;
    
    public UserController(IUserServices userService)
    {
        _userService = userService;
    }

    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateRequest request)
    {
        var response = await _userService.UpdateUserAsync(request);
        if (response == null)
        {
            return BadRequest("Failed to update user profile.");
        }
        return Ok(response);
    }
}