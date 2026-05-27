using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;
using ManufacturingCostManagement.Web.Authorization;
using ManufacturingCostManagement.Web.Localization;
using ManufacturingCostManagement.Web.ViewModels;

namespace ManufacturingCostManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;

        public AccountController(IAuthService authService, IUserService userService, IRoleService roleService, IPermissionService permissionService)
        {
            _authService = authService;
            _userService = userService;
            _roleService = roleService;
            _permissionService = permissionService;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.AuthenticateAsync(model.Username, model.Password);
            if (user == null || user.Role == null)
            {
                ModelState.AddModelError(string.Empty, AppText.Get("Login.InvalidCredentials"));
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.Name),
                new("FullName", user.FullName ?? user.Username)
            };

            // Load role permissions as claims (Admin gets a bypass elsewhere)
            var permissions = await _permissionService.GetCodesForRoleAsync(user.RoleId);
            foreach (var p in permissions) claims.Add(new Claim("permission", p));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity), properties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        [HttpGet, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register()
        {
            ViewBag.Roles = new SelectList(await _roleService.GetAllAsync(), "Id", "Name");
            return View();
        }

        [HttpPost, Authorize(Policy = "AdminOnly"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(await _roleService.GetAllAsync(), "Id", "Name");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                FullName = model.FullName
            };
            await _authService.RegisterAsync(user, model.Password, model.RoleId);
            TempData["Success"] = AppText.Get("Users.CreateBtn") + " ✓ — " + model.Username;
            return RedirectToAction("Index", "Users");
        }

        [HttpGet, Authorize]
        public IActionResult ChangePassword() => View();

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ok = await _authService.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Old password is incorrect.");
                return View(model);
            }
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
