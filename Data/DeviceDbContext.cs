using DeviceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeviceApi.Data
{
    public class DeviceDbContext : DbContext
    {
        public DeviceDbContext(DbContextOptions<DeviceDbContext> options)
            : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceLog> DeviceLogs { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Store boolean flags as 0/1 integers in the database
            var boolToZeroOne = new BoolToZeroOneConverter<int>();
            var activeFlagConverter = new ValueConverter<bool, int>(
                v => v ? 0 : 1,     // active(true) -> 0, inactive(false) -> 1
                v => v == 0         // db 0 => active(true), db 1 => inactive(false)
            );

            modelBuilder.Entity<Device>(entity =>
            {
                entity.ToTable("devices");
                entity.Property(d => d.IsActive)
                      .HasConversion(activeFlagConverter)
                      .HasColumnType("integer");
                entity.Property(d => d.DelFlg)
                      .HasConversion(boolToZeroOne)
                      .HasColumnType("integer");
            });

            modelBuilder.Entity<DeviceLog>().ToTable("device_logs");
            modelBuilder.Entity<AdminUser>().ToTable("admin_users");

            modelBuilder.Entity<AdminUser>().HasData(new AdminUser
            {
                Id = 1,
                Username = "admin",
                // SHA256("valtec")
                PasswordHash = "39f8485ae66793496c7f4e437acfa60d3905653ea01ca155cf1b5d05446f3702",
                Role = "super_admin",
                CreatedAt = new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
