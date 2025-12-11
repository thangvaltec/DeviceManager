using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DeviceApi.Data;
using DeviceApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeviceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminUsersController : ControllerBase
    {
        private readonly DeviceDbContext _context;

        public AdminUsersController(DeviceDbContext context)
        {
            _context = context;
        }

        public class CreateAdminUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = "admin";
            public string Password { get; set; } = string.Empty;
        }

        public class UpdateAdminUserRequest
        {
            public string Role { get; set; } = "admin";
            public string? Password { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.AdminUsers
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var exists = await _context.AdminUsers.AnyAsync(u => u.Username == request.Username);
            if (exists)
            {
                return Conflict("User already exists.");
            }

            var user = new AdminUser
            {
                Username = request.Username,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "admin" : request.Role,
                PasswordHash = ComputeSha256Hash(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Role,
                user.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateAdminUserRequest request)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.Username == "admin" && request.Role != "super_admin")
            {
                return BadRequest("Default admin must remain super_admin.");
            }

            user.Role = string.IsNullOrWhiteSpace(request.Role) ? user.Role : request.Role;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = ComputeSha256Hash(request.Password);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Role,
                user.CreatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.Username == "admin")
            {
                return BadRequest("Default admin user cannot be deleted.");
            }

            _context.AdminUsers.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
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
