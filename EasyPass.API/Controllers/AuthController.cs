using Microsoft.AspNetCore.Mvc;
using EasyPass.API.Services;
using EasyPass.API.Models;

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
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.RegisterAsync(request.Username, request.Pin);
        if (user == null)
            return BadRequest("Username already exists.");

        return Ok(new { user.Id, user.Username });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        LoginResult result = await _userService.LoginAsync(request.Username, request.Pin);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        // Generate JWT token using the authenticated user's ID and username
        string token = _jwtService.GenerateToken(result.User!.Id, result.User!.Username);

        var response = new LoginResponse
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(60)
        };

        return Ok(response);
    }
}
