using Microsoft.AspNetCore.Mvc;
using EasyPass.API.Services;

namespace EasyPass.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public AuthController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = await _userService.RegisterAsync(request.Username, request.Pin);
        if (user == null)
            return BadRequest("Username already exists.");

        return Ok(new { user.Id, user.Username });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.LoginAsync(request.Username, request.Pin);
        if (user == null)
            return Unauthorized("Invalid credentials.");

        // JWT Token creation
        var token = _jwtService.GenerateToken(user.Id, user.Username);

        return Ok(new { token });
    }
}

// DTOs (Data Transfer Objects)
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}
