using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Models.ViewModels.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

public class ProductsController(IProductService products) : Controller
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Index(string search = "")
    {
        ViewBag.Search = search;
        return View(await products.GetProductsAsync(search));
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var product = await products.GetByIdAsync(id);
        return product is null ? NotFound() : View(product);
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public IActionResult Create() => View("Form", new CreateProductViewModel());

    [Authorize(Roles = "ADMIN"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductViewModel model)
    {
        if(!ModelState.IsValid) return View("Form", model);
        var product = await products.CreateAsync(model);
        TempData["SuccessMessage"] = $"{product.Name} was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await products.GetForEditAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [Authorize(Roles = "ADMIN"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditProductViewModel model)
    {
        if(id != model.Id) return BadRequest();
        if(!ModelState.IsValid) return View(model);
        var result = await products.UpdateAsync(id, model);
        if(result.Succeeded) { TempData["SuccessMessage"] = result.Message; return RedirectToAction(nameof(Index)); }
        if(result.RowVersion is not null) model.RowVersion = result.RowVersion;
        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var model = await products.GetForDeleteAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [Authorize(Roles = "ADMIN"), HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, DeleteProductViewModel model)
    {
        if(id != model.Id) return BadRequest();
        var result = await products.DeleteAsync(id, model.RowVersion);
        if(result.Succeeded || result.NotFound)
        {
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError(string.Empty, result.Message);
        var current = await products.GetForDeleteAsync(id);
        return current is null ? RedirectToAction(nameof(Index)) : View("Delete", current);
    }
}
