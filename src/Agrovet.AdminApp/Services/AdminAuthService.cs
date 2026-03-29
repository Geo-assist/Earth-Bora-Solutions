namespace Agrovet.AdminApp.Services;

public class AdminAuthService
{
    public string? Token { get; private set; }
    public string? FullName { get; private set; }
    public string? Role { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token) && Role == "Admin";

    public void Login(string token, string fullName, string role)
    {
        Token = token;
        FullName = fullName;
        Role = role;
    }

    public void Logout()
    {
        Token = null;
        FullName = null;
        Role = null;
    }
}
