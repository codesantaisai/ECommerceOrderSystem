using ECommerceOrderSystem.Data;
using ECommerceOrderSystem.Models.Entities;
using ECommerceOrderSystem.Models.ViewModels.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceOrderSystem.Controllers;

public class ProductsController(ApplicationDbContext dbContext) : Controller
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Index(string search = "")
    {
        var query = dbContext.Products.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(product => product.Name.Contains(search));
        }
        ViewBag.Search = search;
        return View(await query.OrderByDescending(product => product.CreatedDate).ToListAsync());
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) return NotFound();
        return View(product);
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public IActionResult Create() => View("Form", new CreateProductViewModel());

    [Authorize(Roles = "ADMIN"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductViewModel model)
    {
        if(!ModelState.IsValid) return View("Form", model);
        var product = new Product { Name = model.Name.Trim(), Description = model.Description.Trim(), Price = model.Price, Stock = model.Stock, CreatedDate = DateTime.UtcNow };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{product.Name} was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) return NotFound();
        return View(new EditProductViewModel { Id = product.Id, Name = product.Name, Description = product.Description, Price = product.Price, Stock = product.Stock, RowVersion = product.RowVersion });
    }

    [Authorize(Roles = "ADMIN"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditProductViewModel model)
    {
        if(id != model.Id) return BadRequest();
        if(!ModelState.IsValid) return View(model);
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) return NotFound();

        dbContext.Entry(product).Property(item => item.RowVersion).OriginalValue = model.RowVersion;
        product.Name = model.Name.Trim(); product.Description = model.Description.Trim(); product.Price = model.Price; product.Stock = model.Stock;
        try
        {
            await dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{product.Name} was updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch(DbUpdateConcurrencyException exception)
        {
            var databaseValues = await exception.Entries.Single().GetDatabaseValuesAsync();
            if(databaseValues is null) { ModelState.AddModelError(string.Empty, "This product was deleted by another administrator."); return View(model); }
            model.RowVersion = ((Product)databaseValues.ToObject()).RowVersion;
            ModelState.AddModelError(string.Empty, "This product was changed by another administrator. Review your values and save again.");
            return View(model);
        }
    }

    [Authorize(Roles = "ADMIN"), HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) return NotFound();
        return View(new DeleteProductViewModel { Id = product.Id, Name = product.Name, Description = product.Description, Price = product.Price, Stock = product.Stock, RowVersion = product.RowVersion });
    }

    [Authorize(Roles = "ADMIN"), HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, DeleteProductViewModel model)
    {
        if(id != model.Id) return BadRequest();
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) { TempData["ErrorMessage"] = "The product no longer exists."; return RedirectToAction(nameof(Index)); }

        if(await dbContext.OrderItems.AnyAsync(item => item.ProductId == id))
        {
            ModelState.AddModelError(string.Empty, "This product belongs to one or more orders and cannot be deleted because order history must be preserved.");
            model.Name = product.Name; model.Description = product.Description; model.Price = product.Price; model.Stock = product.Stock; model.RowVersion = product.RowVersion;
            return View("Delete", model);
        }

        dbContext.Entry(product).Property(item => item.RowVersion).OriginalValue = model.RowVersion;
        dbContext.Products.Remove(product);
        try
        {
            await dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{product.Name} was deleted successfully.";
        }
        catch(DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "The product was changed or deleted by another administrator. Nothing was deleted.";
        }
        catch(DbUpdateException)
        {
            TempData["ErrorMessage"] = "The product could not be deleted because it is in use.";
        }
        return RedirectToAction(nameof(Index));
    }
}
