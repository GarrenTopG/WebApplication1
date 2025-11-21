using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Database
// ---------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------
// Identity
// ---------------------------
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ---------------------------
// MVC + Razor Pages
// ---------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();

// ---------------------------
// Services
// ---------------------------
builder.Services.AddScoped<ClaimVerificationService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// ---------------------------
// Middleware
// ---------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ---------------------------
// Seed roles & HR user
// ---------------------------
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    // Seed roles
    string[] roles = { "Lecturer", "Coordinator", "Manager", "HR" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed HR account (cannot self-register)
    string hrEmail = "hr@example.com";
    var hrUser = await userManager.FindByEmailAsync(hrEmail);
    if (hrUser == null)
    {
        hrUser = new User
        {
            UserName = "hr",
            Email = hrEmail,
            Role = "HR",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(hrUser, "HrPassword123!"); // Change this in production
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(hrUser, "HR");
        }
    }
}

// ---------------------------
// Default route
// ---------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();

