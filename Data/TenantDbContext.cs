using DeviceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeviceApi.Data
{
    public class TenantDbContext : DbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Store boolean flags as 0/1 integers in the database
            var boolToZeroOne = new BoolToZeroOneConverter<int>();

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("tenants");
                entity.Property(d => d.DelFlg)
                      .HasConversion(boolToZeroOne)
                      .HasColumnType("integer");
            });
        }
    }
}