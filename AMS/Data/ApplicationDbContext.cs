using AMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Ambulance> Ambulances { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<AmbulanceRequest> AmbulanceRequests { get; set; }
    }
}
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