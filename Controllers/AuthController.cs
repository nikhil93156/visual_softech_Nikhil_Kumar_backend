using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Model;
using StudentManagement.Models;
using StudentManagement.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // IConfiguration is used here to potentially load the JWT Secret Key from appsettings.json
        private readonly IConfiguration _config;
        private readonly SqlDataAccessService _dataService;

        public AuthController(IConfiguration config, SqlDataAccessService dataService)
        {
            _config = config;
            _dataService = dataService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequest login)
        {
            // 1. Authenticate user against DB
            if (!_dataService.AuthenticateUser(login.Username, login.Password))
            {
                // Requirement: If login fails, handle it gracefully (frontend uses SweetAlert on 401)
                return Unauthorized(new { message = "Invalid Username or Password." });
            }

            // 2. Create JWT Token on success
            var tokenString = GenerateJwtToken(login.Username);
            return Ok(new { token = tokenString, message = "Login successful" });
        }

        private string GenerateJwtToken(string username)
        {
            // IMPORTANT: This secret key should be stored securely (e.g., in appsettings.json or environment variables)
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsAVerySecretKeyForJWTAuthToken123456789"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "YourAppIssuer",
                audience: "YourAppAudience",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30), // Token valid for 30 minutes
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}