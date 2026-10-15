using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlightTracking.Api.Models;
using FlightTracking.Api.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlightTracking.Api.Services
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
        {
            var expires = DateTime.UtcNow.AddHours(_settings.ExpirationHours);

            // "sub" is what NotificationHub and the controllers read as the user id.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UzerId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.UserName),
                new Claim("role", user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
