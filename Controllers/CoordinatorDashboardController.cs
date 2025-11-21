using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

public class CoordinatorDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public CoordinatorDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("Role") != "Coordinator")
            return RedirectToAction("Login", "Account");

        return View();
    }
}


