using Microsoft.AspNetCore.Identity;

namespace Courses.Models
{
    public class User:IdentityUser
    {
        public string FullName { get; set; }
    }
}
