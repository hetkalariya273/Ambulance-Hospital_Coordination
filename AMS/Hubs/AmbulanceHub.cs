using AMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AMS.Hubs
{
    public class AmbulanceHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public AmbulanceHub(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Driver")]
        public async Task JoinDriverGroup()
        {
            var userId = Context.User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return;
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"driver-{driver.DriverId}");
        }
    }
}
