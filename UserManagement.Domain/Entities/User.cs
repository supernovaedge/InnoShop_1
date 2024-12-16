using Microsoft.AspNetCore.Identity;

namespace UserManagement.Core.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
