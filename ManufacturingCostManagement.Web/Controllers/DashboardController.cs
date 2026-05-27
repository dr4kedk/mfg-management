using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ManufacturingCostManagement.BLL.Interfaces;

namespace ManufacturingCostManagement.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IStatisticsService _statsService;

        public DashboardController(IStatisticsService statsService)
        {
            _statsService = statsService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _statsService.GetDashboardStatsAsync();
            return View(stats);
        }

        public async Task<IActionResult> CostBreakdown()
        {
            var breakdowns = await _statsService.GetProductCostBreakdownsAsync();
            return View(breakdowns);
        }

        public async Task<IActionResult> DepartmentCosts()
        {
            var data = await _statsService.GetDepartmentCostsAsync();
            return View(data);
        }
    }
}
