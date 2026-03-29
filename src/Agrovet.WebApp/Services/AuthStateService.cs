namespace Agrovet.WebApp.Services;

public class AuthStateService
{
    public string? Token { get; private set; }
    public string? FullName { get; private set; }
    public string? Role { get; private set; }
    public string? UserId { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public void Login(string token, string fullName, string role, string userId)
    {
        Token = token;
        FullName = fullName;
        Role = role;
        UserId = userId;
    }

    public void Logout()
    {
        Token = null;
        FullName = null;
        Role = null;
        UserId = null;
    }
}
