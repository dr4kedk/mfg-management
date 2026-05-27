using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize]
    public class MaterialsController : Controller
    {
        private readonly IMaterialService _service;
        private readonly ISupplierService _supplierService;

        public MaterialsController(IMaterialService service, ISupplierService supplierService)
        {
            _service = service;
            _supplierService = supplierService;
        }

        public async Task<IActionResult> Index(string? q, int? supplierId, bool lowStock = false, int page = 1)
        {
            ViewBag.Query = q;
            ViewBag.SupplierId = supplierId;
            ViewBag.LowStock = lowStock;
            ViewBag.Suppliers = new SelectList(await _supplierService.GetAllAsync(), "Id", "Name", supplierId);
            var result = await _service.FilterAsync(q, supplierId, lowStock, page, 10);
            return View(result);
        }

        public async Task<IActionResult> LowStock()
        {
            var items = await _service.GetLowStockAsync();
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var material = await _service.GetByIdAsync(id);
            if (material == null) return NotFound();
            return View(material);
        }

        private async Task LoadSuppliersAsync(int? selected = null)
        {
            var suppliers = await _supplierService.GetAllAsync();
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", selected);
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create()
        {
            await LoadSuppliersAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create(Material model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync(model.SupplierId);
                return View(model);
            }
            await _service.CreateAsync(model);
            TempData["Success"] = "Material created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _service.GetByIdAsync(id);
            if (material == null) return NotFound();
            await LoadSuppliersAsync(material.SupplierId);
            return View(material);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id, Material model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync(model.SupplierId);
                return View(model);
            }
            await _service.UpdateAsync(model);
            TempData["Success"] = "Material updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Material deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
