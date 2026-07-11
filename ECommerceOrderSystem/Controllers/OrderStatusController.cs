using System.Data;
using System.Security.Claims;
using ECommerceOrderSystem.Data;
using ECommerceOrderSystem.Models.Entities;
using ECommerceOrderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceOrderSystem.Controllers;

[Authorize]
public class OrderStatusController(ApplicationDbContext dbContext) : Controller
{
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, OrderStatus status)
    {
        if (status is not (OrderStatus.Shipped or OrderStatus.Delivered))
        {
            TempData["ErrorMessage"] = "Administrators can only update orders to Shipped or Delivered.";
            return RedirectToAction("Details", "Orders", new { id });
        }

        return await ApplyTransitionAsync(id, status, customerId: null);
    }

    [Authorize(Roles = "CUSTOMER")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOwn(Guid id, OrderStatus status)
    {
        if (status is not (OrderStatus.Paid or OrderStatus.Cancelled))
        {
            TempData["ErrorMessage"] = "Customers can only pay for or cancel their own orders.";
            return RedirectToAction("Details", "Orders", new { id });
        }

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await ApplyTransitionAsync(id, status, customerId);
    }

    private async Task<IActionResult> ApplyTransitionAsync(Guid id, OrderStatus status, string? customerId)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var query = dbContext.Orders.Include(order => order.Items).Where(order => order.Id == id);
        if (customerId is not null)
            query = query.Where(order => order.CustomerId == customerId);

        var order = await query.FirstOrDefaultAsync();
        if (order is null)
            return NotFound();

        if (!OrderLifecycleService.TryValidateTransition(order.Status, status, out var error))
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = error;
            return RedirectToAction("Details", "Orders", new { id });
        }

        if (status == OrderStatus.Cancelled)
        {
            var productIds = order.Items.Select(item => item.ProductId).ToList();
            var products = await dbContext.Products.Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id);
            foreach (var item in order.Items)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                    product.Stock += item.Quantity;
            }
        }

        var previousStatus = order.Status;
        order.Status = status;
        order.UpdatedDate = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = status == OrderStatus.Paid
                ? $"Payment recorded for order {order.OrderNumber}."
                : $"Order moved from {previousStatus} to {status}.";
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "The order or its stock changed while the status was being updated. Please try again.";
        }

        return RedirectToAction("Details", "Orders", new { id });
    }
}
