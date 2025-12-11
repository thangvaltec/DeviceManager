using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeviceApi.Migrations
{
    /// <inheritdoc />
    public partial class ConvertBoolFlagsToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert boolean flags to integer (0/1). Active = 0, inactive/deleted = 1.
            migrationBuilder.Sql("""
                ALTER TABLE devices
                ALTER COLUMN "IsActive" TYPE integer
                USING CASE WHEN "IsActive" = TRUE THEN 0 ELSE 1 END;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE devices
                ALTER COLUMN "DelFlg" TYPE integer
                USING CASE WHEN "DelFlg" = TRUE THEN 1 ELSE 0 END;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to boolean flags (true/false). Active = true, inactive/deleted = false.
            migrationBuilder.Sql("""
                ALTER TABLE devices
                ALTER COLUMN "IsActive" TYPE boolean
                USING ("IsActive" = 0);
            """);

            migrationBuilder.Sql("""
                ALTER TABLE devices
                ALTER COLUMN "DelFlg" TYPE boolean
                USING ("DelFlg" = 1);
            """);
        }
    }
}
