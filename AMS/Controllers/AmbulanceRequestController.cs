using AMS.Data;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Controllers
{
    public class AmbulanceRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AmbulanceRequestController(ApplicationDbContext context)
        {
            _context = context;
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

            var request = new AmbulanceRequest
            {
                EmergencyType = model.EmergencyType,
                PatientCondition = model.PatientCondition,
                NumberOfPatients = model.NumberOfPatients,
                PickupLatitude = model.PickupLatitude,
                PickupLongitude = model.PickupLongitude
            };

            _context.AmbulanceRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }

        // GET: AmbulanceRequest/MyRequests
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            var requests = await _context.AmbulanceRequests
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
            var request = await _context.AmbulanceRequests
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
            var request = await _context.AmbulanceRequests
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            // Cancellation is allowed only before the driver reaches the patient
            if (request.Status == AMS.Enums.RequestStatus.Requested ||
                request.Status == AMS.Enums.RequestStatus.Searching ||
                request.Status == AMS.Enums.RequestStatus.Assigned ||
                request.Status == AMS.Enums.RequestStatus.Accepted ||
                request.Status == AMS.Enums.RequestStatus.OnTheWay)
            {
                request.Status = AMS.Enums.RequestStatus.Cancelled;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Request cancelled successfully.";
            }

            return RedirectToAction(nameof(MyRequests));
        }
    }
}