using System.Security.Cryptography;
using System.Text;
using DeviceApi.Data;
using DeviceApi.Models;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _context.AdminUsers
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    role = string.IsNullOrWhiteSpace(u.Role) ? "admin" : u.Role,
                    createdAt = u.CreatedAt
                })
                .ToList();

            return Ok(users);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateAdminUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            if (_context.AdminUsers.Any(u => u.Username == request.Username))
            {
                return Conflict(new { message = "Username already exists." });
            }

            var user = new AdminUser
            {
                Username = request.Username,
                PasswordHash = ComputeSha256(request.Password),
                Role = string.IsNullOrWhiteSpace(request.Role) ? "admin" : request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminUsers.Add(user);
            _context.SaveChanges();

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                role = user.Role,
                createdAt = user.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateAdminUserRequest request)
        {
            var user = _context.AdminUsers.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                user.Role = request.Role;
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = ComputeSha256(request.Password);
            }

            _context.SaveChanges();

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                role = user.Role,
                createdAt = user.CreatedAt
            });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var user = _context.AdminUsers.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Cannot delete the root admin user." });
            }

            _context.AdminUsers.Remove(user);
            _context.SaveChanges();

            return Ok(new { message = "User deleted." });
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
