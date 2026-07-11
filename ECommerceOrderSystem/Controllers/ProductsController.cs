using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

public class ProductsController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => View();

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult Create() => View("Form");

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult Edit(Guid id) => View("Form");
}
