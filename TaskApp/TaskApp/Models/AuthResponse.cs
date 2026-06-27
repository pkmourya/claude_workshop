namespace TaskApp.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int UserId { get; set; }
}
