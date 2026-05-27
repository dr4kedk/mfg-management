using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly IProductService _service;
        private readonly IBomService _bomService;
        private readonly IMaterialService _materialService;

        public ProductsController(IProductService service, IBomService bomService, IMaterialService materialService)
        {
            _service = service;
            _bomService = bomService;
            _materialService = materialService;
        }

        public async Task<IActionResult> Index(string? q, string? category, int page = 1)
        {
            ViewBag.Query = q;
            ViewBag.Category = category;
            ViewBag.Categories = await _service.GetCategoriesAsync();
            var result = await _service.FilterAsync(q, category, page, 10);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _service.GetWithBomAsync(id);
            if (product == null) return NotFound();
            ViewBag.MaterialCost = await _service.CalculateMaterialCostAsync(id);
            return View(product);
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid) return View(model);
            await _service.CreateAsync(model);
            TempData["Success"] = "Product created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id, Product model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            await _service.UpdateAsync(model);
            TempData["Success"] = "Product updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Product deleted.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> AddBom(int productId)
        {
            var product = await _service.GetByIdAsync(productId);
            if (product == null) return NotFound();
            ViewBag.ProductName = product.Name;
            ViewBag.Materials = new SelectList(await _materialService.GetAllAsync(), "Id", "Name");
            return View(new BillOfMaterial { ProductId = productId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> AddBom(BillOfMaterial model)
        {
            if (!ModelState.IsValid)
            {
                var product = await _service.GetByIdAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;
                ViewBag.Materials = new SelectList(await _materialService.GetAllAsync(), "Id", "Name");
                return View(model);
            }
            await _bomService.CreateAsync(model);
            TempData["Success"] = "Material added to BOM.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> RemoveBom(int id, int productId)
        {
            await _bomService.DeleteAsync(id);
            TempData["Success"] = "BOM entry removed.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }
    }
}
