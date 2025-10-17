using EasyPass.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyPass.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilsController : ControllerBase
{
    private readonly PasswordGeneratorService _passwordGenerator;

    public UtilsController(PasswordGeneratorService passwordGenerator)
    {
        _passwordGenerator = passwordGenerator;
    }

    // GET: api - Generate Password
    [HttpGet("generate-password")]
    public IActionResult GeneratePassword([FromQuery] int length = 12, [FromQuery] bool symbols = true)
    {
        var password = _passwordGenerator.Generate(length, symbols);
        return Ok(new { password });
    }
}
