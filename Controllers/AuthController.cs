using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DeviceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeviceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DeviceDbContext _context;

        public AuthController(DeviceDbContext context)
        {
            _context = context;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            var hash = ComputeSha256Hash(request.Password);
            if (!string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized("Invalid username or password.");
            }

            return Ok(new
            {
                username = user.Username,
                role = user.Role
            });
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(rawData);
            var hashBytes = sha256.ComputeHash(bytes);
            var builder = new StringBuilder();
            foreach (var b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
