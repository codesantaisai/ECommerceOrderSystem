using System.Security.Claims;
using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Models.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

[Authorize]
public class OrdersController(IOrderService orders) : Controller
{
    [Authorize(Roles = "CUSTOMER"), HttpGet]
    public async Task<IActionResult> Create(Guid? productId = null) => View(await orders.BuildCreateModelAsync(productId));

    [Authorize(Roles = "CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        if (!ModelState.IsValid) { await orders.PopulateProductDisplayAsync(model); return View(model); }
        var result = await orders.CreateAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Message); await orders.PopulateProductDisplayAsync(model); return View(model); }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.OrderId });
    }

    [Authorize(Roles = "CUSTOMER"), HttpGet]
    public async Task<IActionResult> MyOrders() => View(await orders.GetCustomerOrdersAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

    [Authorize(Roles = "ADMIN"), HttpGet]
    public async Task<IActionResult> All() => View(await orders.GetAllOrdersAsync());

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var order = await orders.GetDetailsAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole("ADMIN"));
        return order is null ? NotFound() : View(order);
    }
}
