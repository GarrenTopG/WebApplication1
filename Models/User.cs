using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class User : IdentityUser
    {
        // Extra property for role reference
        public string Role { get; set; }
    }
}



