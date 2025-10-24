using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class CoordinatorDashboardController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Coordinator")
                return RedirectToAction("Login", "Account");

            return View();
        }
    }
}

