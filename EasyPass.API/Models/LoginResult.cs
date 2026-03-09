namespace EasyPass.API.Models;

// This object is returned by UserService.LoginAsync
// to tell the controller whether login succeeded or failed,
// and what message to show the user.
public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    // The authenticated user — only set when Success is true
    public User? User { get; set; }
}
