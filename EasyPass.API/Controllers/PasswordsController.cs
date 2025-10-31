using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyPass.API.Data;
using EasyPass.API.Models;
using System.Security.Claims;
using EasyPass.API.Services;

namespace EasyPass.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Demanding JWT
public class PasswordsController : ControllerBase
{
    private readonly EasyPassContext _context;
    private readonly EncryptionHelper _encryption;

    public PasswordsController(EasyPassContext context, EncryptionHelper encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    // 🟢 GET: api/passwords
    [HttpGet]
    public async Task<IActionResult> GetPasswords()
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var passwords = await _context.Passwords
            .Where(p => p.UserId == userId)
            .ToListAsync();

        // Decrypt each password before returning
        var decrypted = passwords.Select(p =>
        {
            p.EncryptedPassword = _encryption.Decrypt(p.EncryptedPassword);
            return p;
        }).ToList();

        return Ok(decrypted);
    }

    // 🟢 POST: api/passwords
    [HttpPost]
    public async Task<IActionResult> CreatePassword([FromBody] PasswordEntry entry)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        entry.UserId = userId;

        // Encrypt before saving, client sends plaintext in EncryptedPassword field
        entry.EncryptedPassword = _encryption.Encrypt(entry.EncryptedPassword);

        _context.Passwords.Add(entry);
        await _context.SaveChangesAsync();

        // Return decrypted password value to client for display consistency
        entry.EncryptedPassword = _encryption.Decrypt(entry.EncryptedPassword);
        return Ok(entry);
    }

    // 🟢 PUT: api/passwords/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePassword(int id, [FromBody] PasswordEntry updated)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var password = await _context.Passwords.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (password == null)
            return NotFound();

        password.Service = updated.Service;
        password.Username = updated.Username;
        // Client sends plaintext; encrypt before saving
        password.EncryptedPassword = _encryption.Encrypt(updated.EncryptedPassword);

        await _context.SaveChangesAsync();

        // Return decrypted for client
        password.EncryptedPassword = _encryption.Decrypt(password.EncryptedPassword);
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

        // Decrypt before returning
        var decrypted = results.Select(p =>
        {
            p.EncryptedPassword = _encryption.Decrypt(p.EncryptedPassword);
            return p;
        }).ToList();

        return Ok(decrypted);
    }

}
