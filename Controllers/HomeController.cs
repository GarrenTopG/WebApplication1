using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller //It mainly handles the homepage and a few utility pages.
    {
        private readonly ILogger<HomeController> _logger;
        //It lets you record information about what the app is doing

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        //automatically gives the controller a logger so it can log events.

        // Handles requests to the homepage
        public IActionResult Index()
        {
            return View();
        }

        // Handles requests to the privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Handles errors and shows the error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
