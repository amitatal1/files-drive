// Services/JwtService.cs
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
// Ensure Microsoft.Extensions.Configuration is NOT used here, as we're passing values directly.
// using Microsoft.Extensions.Configuration; // REMOVE THIS IF PRESENT

namespace Server.Services
{
    public class JwtService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(string secret, string issuer, string audience, int expiryMinutes)
        {
            _secret = secret ?? throw new ArgumentNullException(nameof(secret), "JWT Secret cannot be null.");
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer), "JWT Issuer cannot be null.");
            _audience = audience ?? throw new ArgumentNullException(nameof(audience), "JWT Audience cannot be null.");
            _expiryMinutes = expiryMinutes;

            if (string.IsNullOrEmpty(_secret))
            {
                throw new ArgumentException("JWT Secret cannot be empty.", nameof(secret));
            }
            if (string.IsNullOrEmpty(_issuer))
            {
                throw new ArgumentException("JWT Issuer cannot be empty.", nameof(issuer));
            }
            if (string.IsNullOrEmpty(_audience))
            {
                throw new ArgumentException("JWT Audience cannot be empty.", nameof(audience));
            }
            if (_expiryMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expiryMinutes), "JWT ExpiryMinutes must be a positive value.");
            }
        }

        public string GenerateToken(string username)
        {
            // REMOVED Console.WriteLine statements for security.
            // Secrets should not be logged or printed.

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Using 

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username), // Subject claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) 
            };

            var token = new JwtSecurityToken(
                _issuer,
                _audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes), 
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}