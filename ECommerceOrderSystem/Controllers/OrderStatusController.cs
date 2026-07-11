using System.Security.Claims;
using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

[Authorize]
public class OrderStatusController(IOrderService orders) : Controller
{
    [Authorize(Roles = "ADMIN"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, OrderStatus status)
    {
        if (status is not (OrderStatus.Shipped or OrderStatus.Delivered)) return RedirectWithError(id, "Administrators can only update orders to Shipped or Delivered.");
        return await ApplyAsync(id, status);
    }

    [Authorize(Roles = "CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOwn(Guid id, OrderStatus status)
    {
        if (status is not (OrderStatus.Paid or OrderStatus.Cancelled)) return RedirectWithError(id, "Customers can only pay for or cancel their own orders.");
        return await ApplyAsync(id, status, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private async Task<IActionResult> ApplyAsync(Guid id, OrderStatus status, string? customerId = null)
    {
        var result = await orders.UpdateStatusAsync(id, status, customerId);
        if (result.NotFound) return NotFound();
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction("Details", "Orders", new { id });
    }

    private IActionResult RedirectWithError(Guid id, string message)
    {
        TempData["ErrorMessage"] = message;
        return RedirectToAction("Details", "Orders", new { id });
    }
}
