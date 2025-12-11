using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceApi.Models
{
    [Table("tenants")]
    public class Tenant
    {
        [Key]
        public int Id { get; set; }     // PK

        [Required]
        public string TenantCode { get; set; } = string.Empty;

        public string TenantName { get; set; } = string.Empty;

        // Soft delete flag: false = active, true = deleted (hidden)
        public bool DelFlg { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
