using Microsoft.AspNetCore.Mvc;

namespace ManufacturingCostManagement.Web.Controllers
{
    public class DocsController : Controller
    {
        public IActionResult Index() => View();
    }
}
