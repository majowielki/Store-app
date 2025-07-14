using Microsoft.AspNetCore.Identity;

namespace store_app.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
    }
}
