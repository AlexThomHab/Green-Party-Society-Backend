using System.IdentityModel.Tokens.Jwt;
using System.Text;
using GreenPartySocietyAPI.Helpers;
using Microsoft.IdentityModel.Tokens;

namespace GreenPartySocietyAPI.Services
{
    public interface IJwtService
    {
        string Generate(string id, string email, string username, string role);
        IDictionary<string, string>? GetClaims(string jwt);
    }

    public sealed class JwtService : IJwtService
    {
        private readonly string _key;

        public JwtService(IConfiguration config)
        {
            _key = config["Jwt:Key"] ?? throw new Exception("JWT key missing");
        }

        public string Generate(string id, string email, string username, string role)
            => JwtTokenHelper.GenerateToken(id, email, username, role, _key);

        public IDictionary<string, string>? GetClaims(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler()
            {
                MapInboundClaims = false
            };
            var key = Encoding.UTF8.GetBytes(_key);

            try
            {
                var principal = tokenHandler.ValidateToken(
                    jwt,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    },
                    out _);

                return principal.Claims.ToDictionary(c => c.Type, c => c.Value);
            }
            catch
            {
                return null;
            }
        }
    }
}
