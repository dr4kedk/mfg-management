using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _service;

        public SuppliersController(ISupplierService service) { _service = service; }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            ViewBag.Query = q;
            var result = await _service.SearchAsync(q, page, 10);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _service.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create(Supplier model)
        {
            if (!ModelState.IsValid) return View(model);
            await _service.CreateAsync(model);
            TempData["Success"] = "Supplier created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _service.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id, Supplier model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            await _service.UpdateAsync(model);
            TempData["Success"] = "Supplier updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Supplier deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
