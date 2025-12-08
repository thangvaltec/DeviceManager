namespace DeviceApi.Models
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateAdminUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "admin";
    }

    public class UpdateAdminUserRequest
    {
        public string? Password { get; set; }
        public string? Role { get; set; }
    }
}
