using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

[Authorize]
public class OrdersController : Controller
{
    [Authorize(Roles = "CUSTOMER")]
    [HttpGet]
    public IActionResult Create() => View();

    [Authorize(Roles = "CUSTOMER")]
    [HttpGet]
    public IActionResult MyOrders() => View();

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult All() => View();

    [HttpGet]
    public IActionResult Details(Guid? id) => View();
}
