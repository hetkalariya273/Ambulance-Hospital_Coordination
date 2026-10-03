using AMS.Data;
using AMS.Enums;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AMS.Controllers
{
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DriverController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // DRIVER CRUD
        // ============================================================

        // GET: Driver
        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Ambulances)
                .ToListAsync();

            return View(drivers);
        }

        // GET: Driver/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Ambulances)
                .FirstOrDefaultAsync(d => d.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // GET: Driver/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Driver/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DriverCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                UserName = model.Email
            };

            var userResult = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                "Driver");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            var driver = new Driver
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                LicenseNumber = model.LicenseNumber,
                IsAvailable = model.IsAvailable,
                UserId = user.Id
            };

            _context.Drivers.Add(driver);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Driver/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Driver/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Driver driver)
        {
            if (id != driver.DriverId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(driver);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _context.Drivers
                        .AnyAsync(d => d.DriverId == driver.DriverId);

                    if (!exists)
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(driver);
        }

        // GET: Driver/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Driver/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);

            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // DRIVER DASHBOARD
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Ambulances)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            // Get all ambulances assigned to this driver
            var ambulances = await _context.Ambulances
                .Where(a => a.DriverId == driver.DriverId)
                .OrderBy(a => a.VehicleNumber)
                .ToListAsync();

            // Get the driver's current active request
            var currentRequest = await _context.AmbulanceRequests
                .Include(r => r.User)
                .Include(r => r.Ambulance)
                .Include(r => r.Hospital)
                .FirstOrDefaultAsync(r =>
                    r.DriverId == driver.DriverId &&
                    (
                        r.Status == RequestStatus.Accepted ||
                        r.Status == RequestStatus.OnTheWay ||
                        r.Status == RequestStatus.ReachedPatient ||
                        r.Status == RequestStatus.PatientPickedUp ||
                        r.Status == RequestStatus.GoingToHospital ||
                        r.Status == RequestStatus.ReachedHospital
                    ));

            // Get new requests available for acceptance
            var requests = await _context.AmbulanceRequests
                .Include(r => r.User)
                .Where(r =>
                    r.DriverId == null &&
                    r.Status == RequestStatus.Requested)
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();

            var viewModel = new DriverDashboardViewModel
            {
                Driver = driver,
                Ambulances = ambulances,
                CurrentRequest = currentRequest,
                Requests = requests,
                RequestCount = requests.Count
            };

            return View(viewModel);
        }

        // ============================================================
        // DRIVER - REQUEST DETAILS
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpGet]
        public async Task<IActionResult> RequestDetails(int id)
        {
            // Get logged-in Identity user's ID
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Find logged-in driver
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            // Find the requested ambulance request
            var request = await _context.AmbulanceRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id &&
                    r.DriverId == null &&
                    r.Status == RequestStatus.Requested);

            if (request == null)
            {
                TempData["ErrorMessage"] =
                    "This request is no longer available.";

                return RedirectToAction(nameof(Dashboard));
            }

            // Find ambulances assigned to this driver
            // that are currently available
            var availableAmbulances = await _context.Ambulances
                .Where(a =>
                    a.DriverId == driver.DriverId &&
                    a.Status == "Available")
                .OrderBy(a => a.VehicleNumber)
                .ToListAsync();

            // Create Request Details ViewModel
            var viewModel = new RequestDetailsViewModel
            {
                Request = request,
                AvailableAmbulances = availableAmbulances
            };

            return View(viewModel);
        }


        // ============================================================
        // DRIVER - ACCEPT REQUEST
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(
            int id,
            int ambulanceId)
        {
            // Get logged-in Identity user's ID
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Find logged-in driver
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }


            // --------------------------------------------------------
            // Check whether driver already has an active request
            // --------------------------------------------------------

            var existingActiveRequest =
                await _context.AmbulanceRequests
                    .FirstOrDefaultAsync(r =>
                        r.DriverId == driver.DriverId &&
                        (
                            r.Status == RequestStatus.Accepted ||
                            r.Status == RequestStatus.OnTheWay ||
                            r.Status == RequestStatus.ReachedPatient ||
                            r.Status == RequestStatus.PatientPickedUp ||
                            r.Status == RequestStatus.GoingToHospital ||
                            r.Status == RequestStatus.ReachedHospital
                        ));

            if (existingActiveRequest != null)
            {
                TempData["ErrorMessage"] =
                    "You already have an active ambulance request.";

                return RedirectToAction(
                    nameof(Dashboard));
            }

            // --------------------------------------------------------
            // Find the request
            // --------------------------------------------------------

            var request = await _context.AmbulanceRequests
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // Request must still be available
            // --------------------------------------------------------

            if (request.DriverId != null ||
                request.Status != RequestStatus.Requested)
            {
                TempData["ErrorMessage"] =
                    "This request has already been accepted by another driver.";

                return RedirectToAction(nameof(Dashboard));
            }


            // --------------------------------------------------------
            // Find selected ambulance
            // --------------------------------------------------------

            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a =>
                    a.AmbulanceId == ambulanceId &&
                    a.DriverId == driver.DriverId);

            if (ambulance == null)
            {
                TempData["ErrorMessage"] =
                    "The selected ambulance is not assigned to you.";

                return RedirectToAction(
                    nameof(RequestDetails),
                    new { id });
            }


            // --------------------------------------------------------
            // Ambulance must still be available
            // --------------------------------------------------------

            if (ambulance.Status != "Available")
            {
                TempData["ErrorMessage"] =
                    "The selected ambulance is no longer available.";

                return RedirectToAction(
                    nameof(RequestDetails),
                    new { id });
            }


            // --------------------------------------------------------
            // Accept request
            // --------------------------------------------------------

            request.DriverId = driver.DriverId;

            request.AmbulanceId = ambulance.AmbulanceId;

            request.Status = RequestStatus.Accepted;


            // Driver becomes busy
            driver.IsAvailable = false;


            // Selected ambulance becomes busy
            ambulance.Status = "Busy";


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Ambulance request accepted successfully.";

            return RedirectToAction(nameof(Dashboard));
        }


        // ============================================================
        // DRIVER - CURRENT REQUEST DETAILS
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpGet]
        public async Task<IActionResult> CurrentRequestDetails(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            var request = await _context.AmbulanceRequests
                .Include(r => r.User)
                .Include(r => r.Ambulance)
                .Include(r => r.Hospital)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id &&
                    r.DriverId == driver.DriverId &&
                    (
                        r.Status == RequestStatus.Accepted ||
                        r.Status == RequestStatus.OnTheWay ||
                        r.Status == RequestStatus.ReachedPatient ||
                        r.Status == RequestStatus.PatientPickedUp ||
                        r.Status == RequestStatus.GoingToHospital ||
                        r.Status == RequestStatus.ReachedHospital
                    ));

            if (request == null)
            {
                TempData["ErrorMessage"] =
                    "This request is no longer your active request.";

                return RedirectToAction(nameof(Dashboard));
            }

            return View(request);
        }

     
        // ============================================================
        // DRIVER - UPDATE REQUEST STATUS
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(
            int id,
            RequestStatus newStatus)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Find logged-in driver
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            // Find request belonging to this driver
            var request = await _context.AmbulanceRequests
                .Include(r => r.Ambulance)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id &&
                    r.DriverId == driver.DriverId);

            if (request == null)
            {
                return NotFound();
            }

            // Request must be active
            if (!IsActiveStatus(request.Status))
            {
                TempData["ErrorMessage"] =
                    "This request is no longer active.";

                return RedirectToAction(nameof(Dashboard));
            }

            // Validate status transition
            if (!IsValidStatusTransition(
                    request.Status,
                    newStatus))
            {
                TempData["ErrorMessage"] =
                    $"Cannot change request status from " +
                    $"{request.Status} to {newStatus}.";

                return RedirectToAction(
                    nameof(CurrentRequestDetails),
                    new { id = request.RequestId });
            }

            // Update request status
            request.Status = newStatus;

            // Request completed
            if (newStatus == RequestStatus.Completed)
            {
                driver.IsAvailable = true;

                if (request.Ambulance != null)
                {
                    request.Ambulance.Status = "Available";
                }
            }
            else
            {
                // Driver remains busy
                driver.IsAvailable = false;

                if (request.Ambulance != null)
                {
                    request.Ambulance.Status = "Busy";
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Request status updated successfully.";

            // Completed request returns to dashboard
            if (newStatus == RequestStatus.Completed)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            // Continue working on current request
            return RedirectToAction(
                nameof(CurrentRequestDetails),
                new { id = request.RequestId });
        }


        // ============================================================
        // HELPER - ACTIVE REQUEST STATUS
        // ============================================================

        private static bool IsActiveStatus(RequestStatus status)
        {
            return status == RequestStatus.Assigned ||
                   status == RequestStatus.Accepted ||
                   status == RequestStatus.OnTheWay ||
                   status == RequestStatus.ReachedPatient ||
                   status == RequestStatus.PatientPickedUp ||
                   status == RequestStatus.GoingToHospital ||
                   status == RequestStatus.ReachedHospital;
        }


        // ============================================================
        // HELPER - VALID STATUS TRANSITIONS
        // ============================================================

        private static bool IsValidStatusTransition(
            RequestStatus currentStatus,
            RequestStatus newStatus)
        {
            if (currentStatus == RequestStatus.Accepted &&
                newStatus == RequestStatus.OnTheWay)
            {
                return true;
            }

            if (currentStatus == RequestStatus.OnTheWay &&
                newStatus == RequestStatus.ReachedPatient)
            {
                return true;
            }

            if (currentStatus == RequestStatus.ReachedPatient &&
                newStatus == RequestStatus.PatientPickedUp)
            {
                return true;
            }

            if (currentStatus == RequestStatus.PatientPickedUp &&
                newStatus == RequestStatus.GoingToHospital)
            {
                return true;
            }

            if (currentStatus == RequestStatus.GoingToHospital &&
                newStatus == RequestStatus.ReachedHospital)
            {
                return true;
            }

            if (currentStatus == RequestStatus.ReachedHospital &&
                newStatus == RequestStatus.Completed)
            {
                return true;
            }

            return false;
        }
    
    }
}