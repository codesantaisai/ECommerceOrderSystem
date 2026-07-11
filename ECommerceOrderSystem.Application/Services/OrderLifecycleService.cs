using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Domain.Common;

namespace ECommerceOrderSystem.Services;

public sealed class OrderLifecycleService : IOrderLifecycleService
{
    public bool TryValidateTransition(OrderStatus current, OrderStatus next, out string error)
    {
        error = string.Empty;

        if(current == next)
        {
            error = $"The order is already {current}.";
            return false;
        }

        var isValid = (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            _ => false
        };

        if(isValid)
            return true;

        error = next switch
        {
            OrderStatus.Shipped when current != OrderStatus.Paid => "An order can only be shipped after it has been paid.",
            OrderStatus.Delivered when current != OrderStatus.Shipped => "An order can only be delivered after it has been shipped.",
            OrderStatus.Cancelled when current is OrderStatus.Shipped or OrderStatus.Delivered => "An order cannot be cancelled after it has been shipped.",
            OrderStatus.Paid when current != OrderStatus.Pending => "Only a pending order can be marked as paid.",
            _ => $"Changing an order from {current} to {next} is not allowed."
        };
        return false;
    }

    public IReadOnlyList<OrderStatus> GetAllowedTransitions(OrderStatus current) => current switch
    {
        OrderStatus.Pending => [OrderStatus.Paid, OrderStatus.Cancelled],
        OrderStatus.Paid => [OrderStatus.Shipped, OrderStatus.Cancelled],
        OrderStatus.Shipped => [OrderStatus.Delivered],
        _ => []
    };
}

