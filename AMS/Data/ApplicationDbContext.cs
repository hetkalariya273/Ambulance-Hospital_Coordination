using AMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

/* 
  IdentityDbContext already knows how to create/manage Identity tables

    AspNetUsers
    AspNetRoles
    AspNetUserRoles
    AspNetUserClaims
    AspNetUserLogins
    AspNetUserTokens
    AspNetRoleClaims

 */
namespace AMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}