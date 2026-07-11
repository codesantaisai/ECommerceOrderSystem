using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Models.ViewModels.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceOrderSystem.Controllers;

public class ProductsController(IProductService products, ILogger<ProductsController> logger) : Controller
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
        logger.LogInformation("Product {ProductId} ({ProductName}) was created.", product.Id, product.Name);
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
        if(result.Succeeded)
        {
            logger.LogInformation("Product {ProductId} was updated.", id); TempData["SuccessMessage"] = result.Message; return RedirectToAction(nameof(Index));
        }
        logger.LogWarning("Product {ProductId} update failed: {Message}", id, result.Message);
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
            if(result.Succeeded) logger.LogInformation("Product {ProductId} was deleted.", id);
            else logger.LogWarning("Product {ProductId} delete skipped because it was not found.", id);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        logger.LogWarning("Product {ProductId} delete failed: {Message}", id, result.Message);
        ModelState.AddModelError(string.Empty, result.Message);
        var current = await products.GetForDeleteAsync(id);
        return current is null ? RedirectToAction(nameof(Index)) : View("Delete", current);
    }
}
