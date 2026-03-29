using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Agrovet.Domain;
using Agrovet.Domain.Auth;
using Agrovet.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Agrovet.Infrastructure.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    private string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        input = Regex.Replace(input, "<.*?>", string.Empty);
        input = Regex.Replace(input, @"(--|;|/\*|\*/|xp_|EXEC|DROP|INSERT|UPDATE|DELETE|SELECT|UNION|ALTER|CREATE)", string.Empty, RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"<script.*?>.*?</script>", string.Empty, RegexOptions.IgnoreCase);
        return input.Trim();
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private bool IsValidPhone(string phone)
    {
        return Regex.IsMatch(phone, @"^(?:\+254|0)[17]\d{8}$");
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        request.FullName = SanitizeInput(request.FullName);
        request.Email = SanitizeInput(request.Email).ToLower();
        request.PhoneNumber = SanitizeInput(request.PhoneNumber);

        if (!IsValidEmail(request.Email)) return null;
        if (!IsValidPhone(request.PhoneNumber)) return null;
        if (!Regex.IsMatch(request.FullName, @"^[a-zA-Z\s]+$")) return null;

        var existing = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null) return null;

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
        return GenerateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        request.Email = SanitizeInput(request.Email).ToLower();
        if (!IsValidEmail(request.Email)) return null;

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return GenerateToken(user);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        request.Email = SanitizeInput(request.Email).ToLower();
        if (!IsValidEmail(request.Email)) return false;

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    private AuthResponse GenerateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var expiry = DateTime.UtcNow.AddDays(_jwtSettings.ExpiryDays);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            UserId = user.Id,
            Expiry = expiry
        };
    }
}
