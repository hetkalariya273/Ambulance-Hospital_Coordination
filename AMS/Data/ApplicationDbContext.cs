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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser -> Driver
            builder.Entity<Driver>()
                .HasOne(d => d.User)
                .WithOne()
                .HasForeignKey<Driver>(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Driver -> Ambulance
            builder.Entity<Ambulance>()
                .HasOne(a => a.Driver)
                .WithMany(d => d.Ambulances)
                .HasForeignKey(a => a.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            // Driver -> AmbulanceRequest
            builder.Entity<AmbulanceRequest>()
                .HasOne(r => r.Driver)
                .WithMany(d => d.AmbulanceRequests)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.SetNull);
        }
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