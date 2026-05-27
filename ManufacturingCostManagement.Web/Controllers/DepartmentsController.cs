using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentService _service;

        public DepartmentsController(IDepartmentService service) { _service = service; }

        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            ViewBag.Query = q;
            var result = await _service.SearchAsync(q, page, 10);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var dept = await _service.GetByIdAsync(id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create(Department model)
        {
            if (!ModelState.IsValid) return View(model);
            await _service.CreateAsync(model);
            TempData["Success"] = "Department created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id)
        {
            var dept = await _service.GetByIdAsync(id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id, Department model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            await _service.UpdateAsync(model);
            TempData["Success"] = "Department updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Department deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
