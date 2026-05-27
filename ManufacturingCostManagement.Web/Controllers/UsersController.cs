using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : Controller
    {
        private readonly IUserService _service;
        private readonly IRoleService _roleService;

        public UsersController(IUserService service, IRoleService roleService)
        {
            _service = service;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index(string? q, int? roleId, bool? isActive, int page = 1)
        {
            ViewBag.Query = q;
            ViewBag.RoleId = roleId;
            ViewBag.IsActive = isActive;
            ViewBag.Roles = new SelectList(await _roleService.GetAllAsync(), "Id", "Name", roleId);
            var result = await _service.FilterAsync(q, roleId, isActive, page, 10);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _service.GetWithRoleAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _service.GetWithRoleAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = new SelectList(await _roleService.GetAllAsync(), "Id", "Name", user.RoleId);
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(await _roleService.GetAllAsync(), "Id", "Name", model.RoleId);
                return View(model);
            }
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();
            existing.Email = model.Email;
            existing.FullName = model.FullName;
            existing.RoleId = model.RoleId;
            existing.IsActive = model.IsActive;
            await _service.UpdateAsync(existing);
            TempData["Success"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return RedirectToAction(nameof(Details), new { id });
            }
            await _service.ResetPasswordAsync(id, newPassword);
            TempData["Success"] = "Password reset.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
