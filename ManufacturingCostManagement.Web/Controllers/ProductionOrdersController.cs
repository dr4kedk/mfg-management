using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize]
    public class ProductionOrdersController : Controller
    {
        private readonly IProductionOrderService _service;
        private readonly IProductService _productService;
        private readonly ILaborCostService _laborService;
        private readonly IOverheadCostService _overheadService;
        private readonly IDepartmentService _deptService;

        public ProductionOrdersController(
            IProductionOrderService service,
            IProductService productService,
            ILaborCostService laborService,
            IOverheadCostService overheadService,
            IDepartmentService deptService)
        {
            _service = service;
            _productService = productService;
            _laborService = laborService;
            _overheadService = overheadService;
            _deptService = deptService;
        }

        public async Task<IActionResult> Index(string? q, ProductionStatus? status, int? departmentId, int? productId, int page = 1)
        {
            ViewBag.Query = q;
            ViewBag.Status = status;
            ViewBag.DepartmentId = departmentId;
            ViewBag.ProductId = productId;
            ViewBag.Departments = new SelectList(await _deptService.GetAllAsync(), "Id", "Name", departmentId);
            ViewBag.Products = new SelectList(await _productService.GetAllAsync(), "Id", "Name", productId);
            var result = await _service.FilterAsync(q, status, departmentId, productId, page, 10);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _service.GetWithDetailsAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        private async Task LoadProductsAsync(int? selected = null)
        {
            var products = await _productService.GetAllAsync();
            ViewBag.Products = new SelectList(products, "Id", "Name", selected);
        }

        private async Task LoadDepartmentsAsync(int? selected = null)
        {
            var depts = await _deptService.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "Id", "Name", selected);
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create()
        {
            await LoadProductsAsync();
            await LoadDepartmentsAsync();
            return View(new ProductionOrder { OrderCode = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}" });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Create(ProductionOrder model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync(model.ProductId);
                await LoadDepartmentsAsync(model.DepartmentId);
                return View(model);
            }
            await _service.CreateAsync(model);
            TempData["Success"] = "Production order created.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _service.GetByIdAsync(id);
            if (order == null) return NotFound();
            await LoadProductsAsync(order.ProductId);
            await LoadDepartmentsAsync(order.DepartmentId);
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Edit(int id, ProductionOrder model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync(model.ProductId);
                await LoadDepartmentsAsync(model.DepartmentId);
                return View(model);
            }
            await _service.UpdateAsync(model);
            TempData["Success"] = "Order updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Complete(int id)
        {
            await _service.CompleteOrderAsync(id);
            await _service.RecalculateCostsAsync(id);
            TempData["Success"] = "Order completed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ManagerOrAbove")]
        public async Task<IActionResult> Recalculate(int id)
        {
            await _service.RecalculateCostsAsync(id);
            TempData["Success"] = "Costs recalculated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Order deleted.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AccountantOrAbove")]
        public async Task<IActionResult> AddLabor(int orderId)
        {
            var order = await _service.GetByIdAsync(orderId);
            await LoadDepartmentsAsync(order?.DepartmentId);
            return View(new LaborCost { ProductionOrderId = orderId, DepartmentId = order?.DepartmentId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AccountantOrAbove")]
        public async Task<IActionResult> AddLabor(LaborCost model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(model.DepartmentId);
                return View(model);
            }
            model.Amount = model.CalculateTotal();
            await _laborService.CreateAsync(model);
            await _service.RecalculateCostsAsync(model.ProductionOrderId);
            TempData["Success"] = "Labor cost added.";
            return RedirectToAction(nameof(Details), new { id = model.ProductionOrderId });
        }

        [Authorize(Policy = "AccountantOrAbove")]
        public async Task<IActionResult> AddOverhead(int orderId)
        {
            var order = await _service.GetByIdAsync(orderId);
            await LoadDepartmentsAsync(order?.DepartmentId);
            return View(new OverheadCost { ProductionOrderId = orderId, DepartmentId = order?.DepartmentId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AccountantOrAbove")]
        public async Task<IActionResult> AddOverhead(OverheadCost model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(model.DepartmentId);
                return View(model);
            }
            await _overheadService.CreateAsync(model);
            await _service.RecalculateCostsAsync(model.ProductionOrderId);
            TempData["Success"] = "Overhead cost added.";
            return RedirectToAction(nameof(Details), new { id = model.ProductionOrderId });
        }
    }
}
