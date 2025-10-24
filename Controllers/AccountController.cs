using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Try finding user by username first
            var user = await _userManager.FindByNameAsync(model.UsernameOrEmail);
            if (user == null)
            {
                // If not found, try by email
                user = await _userManager.FindByEmailAsync(model.UsernameOrEmail);
            }

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Redirect based on user role
                    switch (user.Role)
                    {
                        case "Lecturer":
                            return RedirectToAction("Index", "Claims"); // ✅ reuse ClaimsController Index view
                        case "Coordinator":
                            return RedirectToAction("Pending", "Claims"); // example: pending claims for coordinator
                        case "Manager":
                            return RedirectToAction("Index", "Claims"); // or a manager-specific page if you have
                        default:
                            return RedirectToAction("Index", "Home");
                    }

                }

            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                Role = model.Role
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Add the user to a role in Identity
                if (!await _userManager.IsInRoleAsync(user, model.Role))
                    await _userManager.AddToRoleAsync(user, model.Role);

                TempData["Message"] = "Registration successful! Please log in.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}


