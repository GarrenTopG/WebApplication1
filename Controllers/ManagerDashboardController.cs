using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class ManagerDashboardController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Manager")
                return RedirectToAction("Login", "Account");

            return View();
        }
    }
}

