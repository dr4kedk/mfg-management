using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ManufacturingCostManagement.Web.Authorization;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PermissionsController : Controller
    {
        private readonly IPermissionService _service;

        public PermissionsController(IPermissionService service) { _service = service; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Roles = await _service.GetAllRolesAsync();
            ViewBag.Permissions = await _service.GetAllAsync();
            ViewBag.Matrix = await _service.GetMatrixAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(IFormCollection form)
        {
            var roles = await _service.GetAllRolesAsync();
            foreach (var role in roles)
            {
                if (role.Name == "Admin") continue; // Admin always implicit ALL
                var key = $"role_{role.Id}";
                var ids = form[key]
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Select(v => int.TryParse(v, out var n) ? n : 0)
                    .Where(n => n > 0)
                    .Distinct()
                    .ToList();
                await _service.SetForRoleAsync(role.Id, ids);
            }
            TempData["Success"] = "Permissions updated. Users must sign out and back in for changes to take effect.";
            return RedirectToAction(nameof(Index));
        }
    }
}
