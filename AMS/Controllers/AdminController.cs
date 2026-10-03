using AMS.Data;
using AMS.Enums;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var activeStatuses = new[]
            {
                RequestStatus.Requested,
                RequestStatus.Searching,
                RequestStatus.Assigned,
                RequestStatus.Accepted,
                RequestStatus.OnTheWay,
                RequestStatus.ReachedPatient,
                RequestStatus.PatientPickedUp,
                RequestStatus.GoingToHospital,
                RequestStatus.ReachedHospital
            };

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),

                TotalDrivers = await _context.Drivers.CountAsync(),

                TotalAmbulances = await _context.Ambulances.CountAsync(),

                TotalHospitals = await _context.Hospitals.CountAsync(),

                ActiveRequests = await _context.AmbulanceRequests
                    .CountAsync(r => activeStatuses.Contains(r.Status))
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager
                .GetUsersInRoleAsync("Patient");

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(user, "Patient"))
            {
                return NotFound();
            }

            var requests = await _context.AmbulanceRequests
                .Where(r => r.UserId == id)
                .ToListAsync();

            var activeStatuses = new[]
            {
                RequestStatus.Requested,
                RequestStatus.Searching,
                RequestStatus.Assigned,
                RequestStatus.Accepted,
                RequestStatus.OnTheWay,
                RequestStatus.ReachedPatient,
                RequestStatus.PatientPickedUp,
                RequestStatus.GoingToHospital,
                RequestStatus.ReachedHospital
            };

            var model = new AdminUserDetailsViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,

                TotalRequests = requests.Count,

                ActiveRequests = requests.Count(r =>
                    activeStatuses.Contains(r.Status)),

                CompletedRequests = requests.Count(r =>
                    r.Status == RequestStatus.Completed),

                CancelledRequests = requests.Count(r =>
                    r.Status == RequestStatus.Cancelled)
            };

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> Requests(string filter = "Active")
        {
            var activeStatuses = new[]
            {
                RequestStatus.Requested,
                RequestStatus.Searching,
                RequestStatus.Assigned,
                RequestStatus.Accepted,
                RequestStatus.OnTheWay,
                RequestStatus.ReachedPatient,
                RequestStatus.PatientPickedUp,
                RequestStatus.GoingToHospital,
                RequestStatus.ReachedHospital
            };

            IQueryable<AmbulanceRequest> query = _context.AmbulanceRequests
                .Include(r => r.User);

            if (filter == "Completed")
            {
                query = query.Where(r =>
                    r.Status == RequestStatus.Completed);
            }
            else if (filter == "Cancelled")
            {
                query = query.Where(r =>
                    r.Status == RequestStatus.Cancelled);
            }
            else
            {
                filter = "Active";

                query = query.Where(r =>
                    activeStatuses.Contains(r.Status));
            }

            var requests = await query
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();

            ViewBag.Filter = filter;

            return View(requests);
        }


        [HttpGet]
        public async Task<IActionResult> RequestDetails(int id)
        {
            var request = await _context.AmbulanceRequests
                .Include(r => r.User)
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
    }
}