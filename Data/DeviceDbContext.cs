using DeviceApi.Models;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Đặt tên bảng trong DB
            modelBuilder.Entity<Device>().ToTable("devices");
            modelBuilder.Entity<DeviceLog>().ToTable("device_logs");
        }
    }
}
