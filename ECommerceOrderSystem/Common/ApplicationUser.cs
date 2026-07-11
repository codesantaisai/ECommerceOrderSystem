using Microsoft.AspNetCore.Identity;

namespace ECommerceOrderSystem.Common
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime LastLogin { get; set; }
    }
}
