using ECommerceOrderSystem.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace ECommerceOrderSystem.Common;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime LastLogin { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}
