using ECommerceOrderSystem.Domain.Common;

namespace ECommerceOrderSystem.Application.Services.Interface;

public interface IOrderLifecycleService
{
    bool TryValidateTransition(OrderStatus current, OrderStatus next, out string error);
    IReadOnlyList<OrderStatus> GetAllowedTransitions(OrderStatus current);
}
