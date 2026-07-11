using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Domain.Common;
using ECommerceOrderSystem.Models.Entities;
using ECommerceOrderSystem.Models.ViewModels.Orders;
using ECommerceOrderSystem.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceOrderSystem.Application.Services;

public sealed class OrderService(ApplicationDbContext db, IOrderLifecycleService lifecycle) : IOrderService
{
    public async Task<CreateOrderViewModel> BuildCreateModelAsync(Guid? selectedProductId = null)
    {
        var products = await db.Products.AsNoTracking().Where(p => p.Stock > 0).OrderBy(p => p.Name).ToListAsync();
        return new CreateOrderViewModel { Items = products.Select(p => new CreateOrderItemViewModel { ProductId = p.Id, ProductName = p.Name, UnitPrice = p.Price, AvailableStock = p.Stock, Quantity = p.Id == selectedProductId ? 1 : 0 }).ToList() };
    }

    public async Task PopulateProductDisplayAsync(CreateOrderViewModel model)
    {
        var products = await db.Products.AsNoTracking().Where(p => p.Stock > 0).OrderBy(p => p.Name).ToDictionaryAsync(p => p.Id);
        foreach(var item in model.Items.Where(i => products.ContainsKey(i.ProductId)))
        {
            var product = products[item.ProductId];
            item.ProductName = product.Name; item.UnitPrice = product.Price; item.AvailableStock = product.Stock;
        }
        model.Items = model.Items.Where(i => products.ContainsKey(i.ProductId)).ToList();
    }

    public async Task<OrderOperationResult> CreateAsync(CreateOrderViewModel model, string customerId)
    {
        var requested = model.Items.Where(i => i.Quantity > 0).ToList();
        if(requested.Count == 0) return new(false, "Select at least one product.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var ids = requested.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach(var item in requested)
        {
            if(!products.TryGetValue(item.ProductId, out var product)) return await Fail(transaction, "One of the selected products is no longer available.");
            if(item.Quantity > product.Stock) return await Fail(transaction, $"Only {product.Stock} unit(s) of {product.Name} are available.");
        }
        var order = new Order { OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}", CustomerId = customerId, ShippingAddress = model.ShippingAddress.Trim(), Status = OrderStatus.Pending, CreatedDate = DateTime.UtcNow };
        foreach(var item in requested)
        {
            var product = products[item.ProductId]; var total = product.Price * item.Quantity;
            product.Stock -= item.Quantity;
            order.Items.Add(new OrderItem { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.Price, LineTotal = total });
            order.GrandTotal += total;
        }
        db.Orders.Add(order);
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); return new(true, $"Order {order.OrderNumber} was created successfully.", order.Id); }
        catch(DbUpdateConcurrencyException) { return await Fail(transaction, "Stock changed while your order was being placed. Review the latest availability and try again."); }
    }

    public async Task<IReadOnlyList<OrderListItemViewModel>> GetCustomerOrdersAsync(string customerId) => await db.Orders.AsNoTracking().Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedDate).Select(o => new OrderListItemViewModel { Id = o.Id, OrderNumber = o.OrderNumber, CreatedDate = o.CreatedDate, ItemCount = o.Items.Sum(i => i.Quantity), GrandTotal = o.GrandTotal, Status = o.Status }).ToListAsync();
    public async Task<IReadOnlyList<OrderListItemViewModel>> GetAllOrdersAsync() => await db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedDate).Select(o => new OrderListItemViewModel { Id = o.Id, OrderNumber = o.OrderNumber, CustomerName = o.Customer.FullName, CreatedDate = o.CreatedDate, ItemCount = o.Items.Sum(i => i.Quantity), GrandTotal = o.GrandTotal, Status = o.Status }).ToListAsync();

    public async Task<OrderDetailsViewModel?> GetDetailsAsync(Guid id, string customerId, bool isAdmin)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.Id == id);
        if(!isAdmin) query = query.Where(o => o.CustomerId == customerId);
        return await query.Select(o => new OrderDetailsViewModel { Id = o.Id, OrderNumber = o.OrderNumber, CustomerName = o.Customer.FullName, CustomerEmail = o.Customer.Email!, CreatedDate = o.CreatedDate, GrandTotal = o.GrandTotal, Status = o.Status, ShippingAddress = o.ShippingAddress, Items = o.Items.Select(i => new OrderDetailsItemViewModel { ProductName = i.Product.Name, Quantity = i.Quantity, UnitPrice = i.UnitPrice, LineTotal = i.LineTotal }).ToList() }).FirstOrDefaultAsync();
    }

    public async Task<OrderOperationResult> UpdateStatusAsync(Guid id, OrderStatus status, string? customerId = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var query = db.Orders.Include(o => o.Items).Where(o => o.Id == id);
        if(customerId is not null) query = query.Where(o => o.CustomerId == customerId);
        var order = await query.FirstOrDefaultAsync();
        if(order is null) return await Fail(transaction, "Order not found.", true);
        if(!lifecycle.TryValidateTransition(order.Status, status, out var error)) return await Fail(transaction, error);
        if(status == OrderStatus.Cancelled)
        {
            var ids = order.Items.Select(i => i.ProductId).ToList();
            var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            foreach(var item in order.Items) if(products.TryGetValue(item.ProductId, out var product)) product.Stock += item.Quantity;
        }
        var previous = order.Status; order.Status = status; order.UpdatedDate = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return new(true, status == OrderStatus.Paid ? $"Payment recorded for order {order.OrderNumber}." : $"Order moved from {previous} to {status}.", order.Id);
        }
        catch(DbUpdateConcurrencyException) { return await Fail(transaction, "The order or its stock changed while the status was being updated. Please try again."); }
    }

    private static async Task<OrderOperationResult> Fail(IDbContextTransaction transaction, string message, bool notFound = false) { await transaction.RollbackAsync(); return new(false, message, null, notFound); }
}

