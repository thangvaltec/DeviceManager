using System.Security.Cryptography;
using System.Text;
using DeviceApi.Data;
using DeviceApi.Models;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            var user = _context.AdminUsers.FirstOrDefault(u => u.Username == request.Username);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            return Ok(new
            {
                username = user.Username,
                role = string.IsNullOrWhiteSpace(user.Role) ? "admin" : user.Role
            });
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            // If stored as SHA256 hex
            var sha256 = ComputeSha256(password);
            if (string.Equals(storedHash, sha256, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback to plain-text match (for legacy data)
            return string.Equals(storedHash, password);
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
