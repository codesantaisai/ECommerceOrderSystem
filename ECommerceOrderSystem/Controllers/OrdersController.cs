using System.Data;
using System.Security.Claims;
using ECommerceOrderSystem.Data;
using ECommerceOrderSystem.Models.Common;
using ECommerceOrderSystem.Models.Entities;
using ECommerceOrderSystem.Models.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceOrderSystem.Controllers;

[Authorize]
public class OrdersController(ApplicationDbContext dbContext) : Controller
{
    [Authorize(Roles = "CUSTOMER"), HttpGet]
    public async Task<IActionResult> Create(Guid? productId = null) =>
        View(await BuildCreateModelAsync(productId));

    [Authorize(Roles = "CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        var requestedItems = model.Items.Where(item => item.Quantity > 0).ToList();
        if(requestedItems.Count == 0)
            ModelState.AddModelError(string.Empty, "Select at least one product.");

        if(!ModelState.IsValid)
        {
            await PopulateProductDisplayAsync(model);
            return View(model);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var productIds = requestedItems.Select(item => item.ProductId).Distinct().ToList();
        var products = await dbContext.Products.Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id);

        foreach(var requestedItem in requestedItems)
        {
            if(!products.TryGetValue(requestedItem.ProductId, out var product))
                ModelState.AddModelError(string.Empty, "One of the selected products is no longer available.");
            else if(requestedItem.Quantity > product.Stock)
                ModelState.AddModelError(string.Empty, $"Only {product.Stock} unit(s) of {product.Name} are available.");
        }

        if(!ModelState.IsValid)
        {
            await transaction.RollbackAsync();
            await PopulateProductDisplayAsync(model);
            return View(model);
        }

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}",
            CustomerId = customerId,
            ShippingAddress = model.ShippingAddress.Trim(),
            Status = OrderStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        foreach(var requestedItem in requestedItems)
        {
            var product = products[requestedItem.ProductId];
            var lineTotal = product.Price * requestedItem.Quantity;
            product.Stock -= requestedItem.Quantity;
            order.Items.Add(new OrderItem { ProductId = product.Id, Quantity = requestedItem.Quantity, UnitPrice = product.Price, LineTotal = lineTotal });
            order.GrandTotal += lineTotal;
        }

        dbContext.Orders.Add(order);
        try
        {
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Order {order.OrderNumber} was created successfully.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch(DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Stock changed while your order was being placed. Review the latest availability and try again.");
            await PopulateProductDisplayAsync(model);
            return View(model);
        }
    }

    [Authorize(Roles = "CUSTOMER"), HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await dbContext.Orders.AsNoTracking().Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedDate)
            .Select(order => new OrderListItemViewModel { Id = order.Id, OrderNumber = order.OrderNumber, CreatedDate = order.CreatedDate, ItemCount = order.Items.Sum(item => item.Quantity), GrandTotal = order.GrandTotal, Status = order.Status })
            .ToListAsync());
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public async Task<IActionResult> All() => View(await dbContext.Orders.AsNoTracking()
        .OrderByDescending(order => order.CreatedDate)
        .Select(order => new OrderListItemViewModel { Id = order.Id, OrderNumber = order.OrderNumber, CustomerName = order.Customer.FullName, CreatedDate = order.CreatedDate, ItemCount = order.Items.Sum(item => item.Quantity), GrandTotal = order.GrandTotal, Status = order.Status })
        .ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = dbContext.Orders.AsNoTracking().Where(order => order.Id == id);
        if(!User.IsInRole("ADMIN")) query = query.Where(order => order.CustomerId == customerId);

        var order = await query.Select(order => new OrderDetailsViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer.FullName,
            CustomerEmail = order.Customer.Email!,
            CreatedDate = order.CreatedDate,
            GrandTotal = order.GrandTotal,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            Items = order.Items.Select(item => new OrderDetailsItemViewModel { ProductName = item.Product.Name, Quantity = item.Quantity, UnitPrice = item.UnitPrice, LineTotal = item.LineTotal }).ToList()
        }).FirstOrDefaultAsync();

        return order is null ? NotFound() : View(order);
    }

    private async Task<CreateOrderViewModel> BuildCreateModelAsync(Guid? selectedProductId = null)
    {
        var products = await dbContext.Products.AsNoTracking().Where(product => product.Stock > 0).OrderBy(product => product.Name).ToListAsync();
        return new CreateOrderViewModel { Items = products.Select(product => new CreateOrderItemViewModel { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, AvailableStock = product.Stock, Quantity = product.Id == selectedProductId ? 1 : 0 }).ToList() };
    }

    private async Task PopulateProductDisplayAsync(CreateOrderViewModel model)
    {
        var products = await dbContext.Products.AsNoTracking().Where(product => product.Stock > 0).OrderBy(product => product.Name).ToDictionaryAsync(product => product.Id);
        foreach(var item in model.Items)
        {
            if(products.TryGetValue(item.ProductId, out var product)) { item.ProductName = product.Name; item.UnitPrice = product.Price; item.AvailableStock = product.Stock; }
        }
        model.Items = model.Items.Where(item => products.ContainsKey(item.ProductId)).ToList();
    }
}
