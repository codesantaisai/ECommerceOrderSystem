using ECommerceOrderSystem.Domain.Common;
using ECommerceOrderSystem.Models.ViewModels.Orders;

namespace ECommerceOrderSystem.Application.Services.Interface;

public interface IOrderService
{
    Task<CreateOrderViewModel> BuildCreateModelAsync(Guid? selectedProductId = null);
    Task PopulateProductDisplayAsync(CreateOrderViewModel model);
    Task<OrderOperationResult> CreateAsync(CreateOrderViewModel model, string customerId);
    Task<IReadOnlyList<OrderListItemViewModel>> GetCustomerOrdersAsync(string customerId);
    Task<IReadOnlyList<OrderListItemViewModel>> GetAllOrdersAsync();
    Task<OrderDetailsViewModel?> GetDetailsAsync(Guid id, string customerId, bool isAdmin);
    Task<OrderOperationResult> UpdateStatusAsync(Guid id, OrderStatus status, string? customerId = null);
}

public sealed record OrderOperationResult(bool Succeeded, string Message, Guid? OrderId = null, bool NotFound = false);
