// src/TelegramBot.API/Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using TelegramBot.Infrastructure.Services;

namespace TelegramBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService) =>
        _userService = userService;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] bool? blocked = null)
    {
        var users = await _userService.GetAllUsersAsync(page, limit, blocked);
        return Ok(users);
    }

    [HttpPatch("{telegramId}/block")]
    public async Task<IActionResult> ToggleBlock(string telegramId, [FromBody] BlockRequest req)
    {
        var success = await _userService.SetBlockedAsync(telegramId, req.IsBlocked);
        if (!success) return NotFound();
        return Ok(new { success = true, message = req.IsBlocked ? "Engellendi" : "Engel kaldırıldı" });
    }
}

public record BlockRequest(bool IsBlocked);