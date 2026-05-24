using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace PaymentGateway.Api.Controllers
{
    public record LoginRequest(string Email, string Password);
    public record User(string Email, string Password, string Role);

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        // In a real application, you would use a database and proper password hashing or an identity provider,
        // but for this demo, we'll use a hardcoded list of users
        private static readonly List<User> Users =
        [
            new("admin@test.com", "password123", "Merchant"),
            new("user@test.com", "password123", "User")
        ];

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var user = Users.SingleOrDefault(x =>
                x.Email == request.Email &&
                x.Password == request.Password);

            if (user is null)
                return Unauthorized("Invalid credentials");

            var token = GenerateToken(user);

            return Ok(new
            {
                accessToken = token,
                role = user.Role
            });
        }

        private string GenerateToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)
            );

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // expiry time is hardcoded to an hour but this could be added to appsettings etc
                signingCredentials: new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
