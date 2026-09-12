using Microsoft.AspNetCore.Identity;

/*IdentityUser already provides things like:
 
Id
UserName
Email
PasswordHash
PhoneNumber
...
*/

namespace AMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}