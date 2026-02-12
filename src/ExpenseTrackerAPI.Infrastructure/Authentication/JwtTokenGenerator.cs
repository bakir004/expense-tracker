using ExpenseTrackerAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ExpenseTrackerAPI.Infrastructure.Authentication;

/// <summary>
/// JWT token generator implementation using System.IdentityModel.Tokens.Jwt.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public int TokenExpirationHours
    {
        get
        {
            var value = _configuration["Jwt:ExpirationHours"];
            return int.TryParse(value, out var hours) ? hours : 24;
        }
    }

    public string GenerateToken(int userId, string email, string name)
    {
        var jwtSettings = GetJwtSettings();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim("userId", userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(TokenExpirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private JwtSettings GetJwtSettings()
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT secret key is not configured. Please set 'Jwt:SecretKey' in configuration.");
        }

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT secret key must be at least 32 characters long for security.");
        }

        return new JwtSettings
        {
            SecretKey = secretKey,
            Issuer = _configuration["Jwt:Issuer"] ?? "ExpenseTrackerAPI",
            Audience = _configuration["Jwt:Audience"] ?? "ExpenseTrackerAPI-Users"
        };
    }

    private class JwtSettings
    {
        public required string SecretKey { get; init; }
        public required string Issuer { get; init; }
        public required string Audience { get; init; }
    }
}
