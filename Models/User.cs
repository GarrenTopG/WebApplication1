using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class User : IdentityUser
    {
        // Stores role as a simple string, also managed by IdentityRole
        public string Role { get; set; } = string.Empty;
    }
}




