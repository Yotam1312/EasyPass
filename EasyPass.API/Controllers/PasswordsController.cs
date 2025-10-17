using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyPass.API.Data;
using EasyPass.API.Models;
using System.Security.Claims;

namespace EasyPass.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Demanding JWT
public class PasswordsController : ControllerBase
{
    private readonly EasyPassContext _context;

    public PasswordsController(EasyPassContext context)
    {
        _context = context;
    }

    // 🟢 GET: api/passwords
    [HttpGet]
    public async Task<IActionResult> GetPasswords()
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var passwords = await _context.Passwords
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return Ok(passwords);
    }

    // 🟢 POST: api/passwords
    [HttpPost]
    public async Task<IActionResult> CreatePassword([FromBody] PasswordEntry entry)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        entry.UserId = userId;

        _context.Passwords.Add(entry);
        await _context.SaveChangesAsync();

        return Ok(entry);
    }

    // 🟢 PUT: api/passwords/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePassword(int id, [FromBody] PasswordEntry updated)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var password = await _context.Passwords.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (password == null)
            return NotFound();

        password.Service = updated.Service;
        password.Username = updated.Username;
        password.EncryptedPassword = updated.EncryptedPassword;

        await _context.SaveChangesAsync();
        return Ok(password);
    }

    // 🟢 DELETE: api/passwords/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePassword(int id)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var password = await _context.Passwords.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (password == null)
            return NotFound();

        _context.Passwords.Remove(password);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // 🟢 GET: api/passwords/search?service=gmail
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string service)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var results = await _context.Passwords
            .Where(p => p.UserId == userId && p.Service.Contains(service))
            .ToListAsync();

        if (!results.Any())
            return NotFound("No passwords found for that service.");

        return Ok(results);
    }

}
