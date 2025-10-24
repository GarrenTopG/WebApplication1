using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class LecturerDashboardController : Controller
    {
        public IActionResult Index()
        {
            // Only allow if user is Lecturer
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Login", "Account");

            return View();
        }
    }
}

