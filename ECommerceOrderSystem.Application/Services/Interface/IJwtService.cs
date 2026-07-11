using ECommerceOrderSystem.Common;

namespace ECommerceOrderSystem.Application.Services.Interface;

public interface IJwtService
{
    Task<(string Token, DateTime ExpiresAt)> GenerateToken(ApplicationUser user);
}
