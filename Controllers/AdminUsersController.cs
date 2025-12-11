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
        private readonly TenantDbContext _masterDb;
        private readonly TenantDbContextFactory _factory;

        public AdminUsersController(
            TenantDbContext masterDb,
            TenantDbContextFactory factory)
        {
            _masterDb = masterDb;
            _factory = factory;
        }

        // ★ マルチテナント対応：tenantCode からテナントDB用 DeviceDbContext を取得
        private DeviceDbContext GetTenantDb()
        {
            // ① リクエストヘッダーまたはJWTから tenantCode を取得
            var tenantCode = User.FindFirst("tenantCode")?.Value;
            
            // ② ヘッダーから取得を試みる（JWT未実装時の暫定対応）
            if (string.IsNullOrWhiteSpace(tenantCode))
            {
                tenantCode = Request.Headers["X-Tenant-Code"].ToString();
            }

            // ③ クエリパラメーターから取得を試みる（デバッグ用）
            if (string.IsNullOrWhiteSpace(tenantCode))
            {
                tenantCode = Request.Query["tenantCode"].ToString();
            }

            // ④ Cookie から取得を試みる（ログイン後の自動保存）
            if (string.IsNullOrWhiteSpace(tenantCode))
            {
                Request.Cookies.TryGetValue("tenantCode", out tenantCode);
            }
            
            if (string.IsNullOrWhiteSpace(tenantCode))
                throw new Exception("tenantCode が JWT、ヘッダー(X-Tenant-Code)、クエリパラメータ(tenantCode)、または Cookie に含まれていません");

            // ⑤ masterDB から接続先情報を取得
            var tenant = _masterDb.Tenants.FirstOrDefault(t => t.TenantCode == tenantCode);
            if (tenant == null)
                throw new Exception("MasterDB にテナント情報がありません");

            // ⑥ テナントDB用の接続文字列
            string connStr = $"Host=localhost;Port=5432;" + $"Database={tenant.TenantCode};" + $"Username=postgres;Password=Valtec;SslMode=Disable;";

            // ⑦ 動的に DeviceDbContext を生成
            return _factory.Create(connStr);
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

        public class UpdateDeviceRequest
        {
            public string SerialNo { get; set; } = "";
            public int AuthMode { get; set; }
            public string DeviceName { get; set; } = "";
            public bool IsActive { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            using var db = GetTenantDb();

            var users = await db.AdminUsers
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
            using var db = GetTenantDb();
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var exists = await db.AdminUsers.AnyAsync(u => u.Username == request.Username);
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

            db.AdminUsers.Add(user);
            await db.SaveChangesAsync();

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
            using var db = GetTenantDb();
            var user = await db.AdminUsers.FindAsync(id);
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

            await db.SaveChangesAsync();

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
            using var db = GetTenantDb();
            var user = await db.AdminUsers.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.Username == "admin")
            {
                return BadRequest("Default admin user cannot be deleted.");
            }

            db.AdminUsers.Remove(user);
            await db.SaveChangesAsync();

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
