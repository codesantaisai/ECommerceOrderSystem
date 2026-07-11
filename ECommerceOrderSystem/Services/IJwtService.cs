using ECommerceOrderSystem.Common;

namespace ECommerceOrderSystem.Services;

public interface IJwtService
{
    Task<(string Token, DateTime ExpiresAt)> GenerateToken(ApplicationUser user);
}
