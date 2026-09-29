using AMS.Data;
using AMS.Enums;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}