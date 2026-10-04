using AMS.Data;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AMS.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AMS.Controllers
{
    [Authorize(Roles = "Patient")]
    public class AmbulanceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<AmbulanceHub> _hubContext;

        public AmbulanceRequestController(
            ApplicationDbContext context,
            IHubContext<AmbulanceHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: AmbulanceRequest/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: AmbulanceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken] //It checks whether the request is coming from a legitimate source and not from a malicious site.
        public async Task<IActionResult> Create(AmbulanceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //A claim is a piece of information about the authenticated user.
            // In Identity, Id  is used to uniquely identify the user so ClaimTypes.NameIdemtifier returns Id.             
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = new AmbulanceRequest
            {
                UserId = userId,
                EmergencyType = model.EmergencyType,
                PatientCondition = model.PatientCondition,
                NumberOfPatients = model.NumberOfPatients,
                PickupLatitude = model.PickupLatitude,
                PickupLongitude = model.PickupLongitude
            };

            _context.AmbulanceRequests.Add(request);
            await _context.SaveChangesAsync();

            var availableAmbulances = await _context.Ambulances
                .Where(a =>
                    a.Status == "Available" &&
                    a.DriverId != null &&
                    a.Latitude.HasValue &&
                    a.Longitude.HasValue)
                .ToListAsync();

            var nearbyDriverIds = availableAmbulances
                .Where(ambulance =>
                    CalculateDistanceInKm(
                        ambulance.Latitude!.Value,
                        ambulance.Longitude!.Value,
                        request.PickupLatitude,
                        request.PickupLongitude) <= 5.0)
                .Select(ambulance => ambulance.DriverId!.Value)
                .Distinct()
                .ToList();

            foreach (var driverId in nearbyDriverIds)
            {
                await _hubContext.Clients
                    .Group($"driver-{driverId}")
                    .SendAsync(
                        "NewAmbulanceRequest",
                        request.RequestId);
            }

            return RedirectToAction(nameof(MyRequests));
        }

        // GET: AmbulanceRequest/MyRequests
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requests = await _context.AmbulanceRequests
                .Where(r => r.UserId == userId)
                .Include(r => r.Driver)
                .Include(r => r.Ambulance)
                .Include(r => r.Hospital)
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();

            return View(requests);
        }

        // GET: AmbulanceRequest/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = await _context.AmbulanceRequests
                .Where(r => r.RequestId == id && r.UserId == userId)
                .Include(r => r.Driver)
                .Include(r => r.Ambulance)
                .Include(r => r.Hospital)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // POST: AmbulanceRequest/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            //Added userId so that another usen can't cancle anyother user's request using requestId only.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var request = await _context.AmbulanceRequests
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id &&
                    r.UserId == userId);

            if (request == null)
            {
                return NotFound();
            }

            // Cancellation is allowed only before the driver reaches the patient
            if (request.Status == AMS.Enums.RequestStatus.Requested ||
                request.Status == AMS.Enums.RequestStatus.Accepted ||
                request.Status == AMS.Enums.RequestStatus.OnTheWay)
            {
                request.Status = AMS.Enums.RequestStatus.Cancelled;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Request cancelled successfully.";
            }

            return RedirectToAction(nameof(MyRequests));
        }

        private static double CalculateDistanceInKm(
    double latitude1,
    double longitude1,
    double latitude2,
    double longitude2)
        {
            const double earthRadiusKm = 6371.0;

            double lat1Rad = latitude1 * Math.PI / 180.0;
            double lat2Rad = latitude2 * Math.PI / 180.0;

            double deltaLat =
                (latitude2 - latitude1) * Math.PI / 180.0;

            double deltaLon =
                (longitude2 - longitude1) * Math.PI / 180.0;

            double a =
                Math.Sin(deltaLat / 2) *
                Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) *
                Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) *
                Math.Sin(deltaLon / 2);

            double c =
                2 * Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }
    }
}