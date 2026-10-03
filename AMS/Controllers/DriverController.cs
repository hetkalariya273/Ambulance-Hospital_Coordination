using AMS.Data;
using AMS.Enums;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AMS.Controllers
{
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create(Driver driver)
        {
            if (ModelState.IsValid)
            {
                _context.Add(driver);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(driver);
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
            // --------------------------------------------------------
            // 1. Get logged-in Identity user's ID
            // --------------------------------------------------------

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // 2. Find Driver profile linked to this Identity account
            // --------------------------------------------------------

            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Ambulances)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            // --------------------------------------------------------
            // 3. Find ambulance assigned to this driver
            // --------------------------------------------------------

            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a =>
                    a.DriverId == driver.DriverId);

            // --------------------------------------------------------
            // 4. Find driver's active request
            //
            // A driver is allowed to have ONLY ONE request
            // in any of these active states.
            // --------------------------------------------------------

            var activeRequest = await _context.AmbulanceRequests
                .Include(r => r.User)
                .Include(r => r.Ambulance)
                .Include(r => r.Hospital)
                .FirstOrDefaultAsync(r =>
                    r.DriverId == driver.DriverId &&
                    IsActiveStatus(r.Status));

            // --------------------------------------------------------
            // 5. Safety synchronization
            //
            // If an active request exists, driver is busy.
            // Otherwise driver is available.
            // --------------------------------------------------------

            var shouldBeAvailable = activeRequest == null;

            if (driver.IsAvailable != shouldBeAvailable)
            {
                driver.IsAvailable = shouldBeAvailable;
                await _context.SaveChangesAsync();
            }

            // --------------------------------------------------------
            // 6. Create dashboard ViewModel
            // --------------------------------------------------------

            var viewModel = new DriverDashboardViewModel
            {
                Driver = driver,
                Ambulance = ambulance,
                ActiveRequest = activeRequest,
                ActiveRequestCount = activeRequest == null ? 0 : 1
            };

            return View(viewModel);
        }


        // ============================================================
        // DRIVER - ACCEPT REQUEST
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // Find logged-in driver
            // --------------------------------------------------------

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            // --------------------------------------------------------
            // Check whether driver already has another active request
            // --------------------------------------------------------

            var existingActiveRequest =
                await _context.AmbulanceRequests
                    .FirstOrDefaultAsync(r =>
                        r.DriverId == driver.DriverId &&
                        IsActiveStatus(r.Status));

            if (existingActiveRequest != null &&
                existingActiveRequest.RequestId != id)
            {
                TempData["ErrorMessage"] =
                    "You already have an active ambulance request.";

                return RedirectToAction(nameof(Dashboard));
            }

            // --------------------------------------------------------
            // Find the request
            // --------------------------------------------------------

            var request = await _context.AmbulanceRequests
                .Include(r => r.Ambulance)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // Request must be assigned to this driver
            // --------------------------------------------------------

            if (request.DriverId != driver.DriverId)
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // Request must currently be Assigned
            // --------------------------------------------------------

            if (request.Status != RequestStatus.Assigned)
            {
                TempData["ErrorMessage"] =
                    "This request cannot be accepted in its current state.";

                return RedirectToAction(nameof(Dashboard));
            }

            // --------------------------------------------------------
            // Accept request
            // --------------------------------------------------------

            request.Status = RequestStatus.Accepted;

            driver.IsAvailable = false;

            if (request.Ambulance != null)
            {
                request.Ambulance.Status = "Busy";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Ambulance request accepted successfully.";

            return RedirectToAction(nameof(Dashboard));
        }


        // ============================================================
        // DRIVER - UPDATE REQUEST STATUS
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(
            int id,
            RequestStatus status)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // Find driver
            // --------------------------------------------------------

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound(
                    "Driver profile is not linked with this account.");
            }

            // --------------------------------------------------------
            // Find request belonging to this driver
            // --------------------------------------------------------

            var request = await _context.AmbulanceRequests
                .Include(r => r.Ambulance)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id &&
                    r.DriverId == driver.DriverId);

            if (request == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // Validate status transition
            // --------------------------------------------------------

            if (!IsValidStatusTransition(
                    request.Status,
                    status))
            {
                TempData["ErrorMessage"] =
                    $"Cannot change request status from " +
                    $"{request.Status} to {status}.";

                return RedirectToAction(nameof(Dashboard));
            }

            // --------------------------------------------------------
            // Update status
            // --------------------------------------------------------

            request.Status = status;

            // --------------------------------------------------------
            // Request completed/cancelled
            // Driver becomes available again
            // --------------------------------------------------------

            if (status == RequestStatus.Completed ||
                status == RequestStatus.Cancelled)
            {
                driver.IsAvailable = true;

                if (request.Ambulance != null)
                {
                    request.Ambulance.Status = "Available";
                }
            }
            else
            {
                // Any active request keeps driver busy
                driver.IsAvailable = false;

                if (request.Ambulance != null)
                {
                    request.Ambulance.Status = "Busy";
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Request status updated successfully.";

            return RedirectToAction(nameof(Dashboard));
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
            // Driver accepts assigned request
            if (currentStatus == RequestStatus.Assigned &&
                newStatus == RequestStatus.Accepted)
            {
                return true;
            }

            // Accepted -> OnTheWay
            if (currentStatus == RequestStatus.Accepted &&
                newStatus == RequestStatus.OnTheWay)
            {
                return true;
            }

            // OnTheWay -> ReachedPatient
            if (currentStatus == RequestStatus.OnTheWay &&
                newStatus == RequestStatus.ReachedPatient)
            {
                return true;
            }

            // ReachedPatient -> PatientPickedUp
            if (currentStatus == RequestStatus.ReachedPatient &&
                newStatus == RequestStatus.PatientPickedUp)
            {
                return true;
            }

            // PatientPickedUp -> GoingToHospital
            if (currentStatus == RequestStatus.PatientPickedUp &&
                newStatus == RequestStatus.GoingToHospital)
            {
                return true;
            }

            // GoingToHospital -> ReachedHospital
            if (currentStatus == RequestStatus.GoingToHospital &&
                newStatus == RequestStatus.ReachedHospital)
            {
                return true;
            }

            // ReachedHospital -> Completed
            if (currentStatus == RequestStatus.ReachedHospital &&
                newStatus == RequestStatus.Completed)
            {
                return true;
            }

            return false;
        }
    }
}