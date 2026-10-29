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


        [Authorize]
        public async Task JoinRequestTrackingGroup(int requestId)
        {
            var userId = Context.User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return;

            var request = await _context.AmbulanceRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return;

            bool isPatient = request.UserId == userId;

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            bool isAssignedDriver =
                driver != null &&
                request.DriverId == driver.DriverId;

            if (!isPatient && !isAssignedDriver)
                return;

            var activeStatuses = new[]
            {
        AMS.Enums.RequestStatus.Accepted,
        AMS.Enums.RequestStatus.OnTheWay,
        AMS.Enums.RequestStatus.ReachedPatient,
        AMS.Enums.RequestStatus.PatientPickedUp,
        AMS.Enums.RequestStatus.GoingToHospital,
        AMS.Enums.RequestStatus.ReachedHospital
    };

            if (!activeStatuses.Contains(request.Status))
                return;

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"request-{requestId}");
        }

    }
}
