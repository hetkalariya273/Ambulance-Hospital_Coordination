using AMS.Data;
using AMS.Enums;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AMS.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AMS.Controllers
{
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<AmbulanceHub> _hubContext;

        public DriverController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<AmbulanceHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
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
            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Ambulances)
                .FirstOrDefaultAsync(d => d.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            // Check whether driver has an active request
            var hasActiveRequest = await _context.AmbulanceRequests
                .AnyAsync(r =>
                    r.DriverId == driver.DriverId &&
                    (
                        r.Status == RequestStatus.Accepted ||
                        r.Status == RequestStatus.OnTheWay ||
                        r.Status == RequestStatus.ReachedPatient ||
                        r.Status == RequestStatus.PatientPickedUp ||
                        r.Status == RequestStatus.GoingToHospital ||
                        r.Status == RequestStatus.ReachedHospital
                    ));

            if (hasActiveRequest)
            {
                TempData["ErrorMessage"] =
                    "This driver cannot be deleted because they have an active ambulance request.";

                return RedirectToAction(nameof(Index));
            }

            // Unassign all ambulances from this driver
            foreach (var ambulance in driver.Ambulances)
            {
                ambulance.DriverId = null;
                ambulance.Status = "Available";
            }

            // Store linked Identity user before deleting Driver
            var applicationUser = driver.User;

            // Delete Driver profile
            _context.Drivers.Remove(driver);

            // Delete Identity account
            if (applicationUser != null)
            {
                _context.Users.Remove(applicationUser);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Driver deleted successfully.";

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
            // Get all requested ambulance requests
            var requests = await GetNearbyRequests(driver.DriverId);


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


        [Authorize(Roles = "Driver")]
        [HttpGet]
        public async Task<IActionResult> NearbyRequests()
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

            var requests = await GetNearbyRequests(
                driver.DriverId);

            return PartialView(
                "_NearbyRequests",
                requests);
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
            // Start serializable transaction
            // --------------------------------------------------------

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // ----------------------------------------------------
                // Find request inside transaction
                // ----------------------------------------------------

                var request = await _context.AmbulanceRequests
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == id);

                if (request == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound();
                }

                // ----------------------------------------------------
                // Request must still be available
                // ----------------------------------------------------

                if (request.DriverId != null ||
                    request.Status != RequestStatus.Requested)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "This request has already been accepted by another driver.";

                    return RedirectToAction(nameof(Dashboard));
                }

                // ----------------------------------------------------
                // Find selected ambulance
                // ----------------------------------------------------

                var ambulance = await _context.Ambulances
                    .FirstOrDefaultAsync(a =>
                        a.AmbulanceId == ambulanceId &&
                        a.DriverId == driver.DriverId);

                if (ambulance == null)
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "The selected ambulance is not assigned to you.";

                    return RedirectToAction(
                        nameof(RequestDetails),
                        new { id });
                }

                // ----------------------------------------------------
                // Ambulance must still be available
                // ----------------------------------------------------

                if (ambulance.Status != "Available")
                {
                    await transaction.RollbackAsync();

                    TempData["ErrorMessage"] =
                        "The selected ambulance is no longer available.";

                    return RedirectToAction(
                        nameof(RequestDetails),
                        new { id });
                }

                // ----------------------------------------------------
                // Accept request
                // ----------------------------------------------------

                request.DriverId = driver.DriverId;

                request.AmbulanceId = ambulance.AmbulanceId;

                request.Status = RequestStatus.Accepted;

                // Driver becomes busy
                driver.IsAvailable = false;

                // Selected ambulance becomes busy
                ambulance.Status = "Busy";

                // ----------------------------------------------------
                // Save all changes
                // ----------------------------------------------------

                await _context.SaveChangesAsync();

                // ----------------------------------------------------
                // Commit transaction
                // ----------------------------------------------------

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Ambulance request accepted successfully.";

                return RedirectToAction(nameof(Dashboard));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Unable to accept the ambulance request. Please try again.";

                return RedirectToAction(nameof(Dashboard));
            }
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

            var hospitals = await _context.Hospitals
                .Where(h =>
                (
                    request.PatientCondition == PatientCondition.Critical &&
                    h.EmergencyAvailable &&
                    h.AvailableICUBeds >= request.NumberOfPatients
                )
                ||
                (
                    request.PatientCondition == PatientCondition.Unconscious &&
                    h.EmergencyAvailable &&
                    h.AvailableICUBeds >= request.NumberOfPatients
                )
                ||
                (
                    request.PatientCondition == PatientCondition.Serious &&
                    h.EmergencyAvailable &&
                    h.AvailableBeds >= request.NumberOfPatients
                )
                ||
                (
                    request.PatientCondition == PatientCondition.Stable &&
                    h.AvailableBeds >= request.NumberOfPatients
                )
            )
            .OrderBy(h => h.Name)
            .ToListAsync();

            hospitals = hospitals
                .OrderBy(h =>
                    CalculateDistanceInKm(
                        request.PickupLatitude,
                        request.PickupLongitude,
                        h.Latitude!.Value,
                        h.Longitude!.Value))
                .ToList();

            var model = new CurrentRequestDetailsViewModel
            {
                Request = request,
                Hospitals = hospitals
            };

            return View(model);
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

            // ------------------------------------------------------------
            // Hospital must be selected before going to hospital
            // ------------------------------------------------------------

            if (request.Status == RequestStatus.PatientPickedUp &&
                newStatus == RequestStatus.GoingToHospital)
            {
                if (request.HospitalId == null)
                {
                    TempData["ErrorMessage"] =
                        "Please select a hospital before going to the hospital.";

                    return RedirectToAction(
                        nameof(CurrentRequestDetails),
                        new { id = request.RequestId });
                }

                // Find selected hospital
                var hospital = await _context.Hospitals
                    .FirstOrDefaultAsync(h =>
                        h.HospitalId == request.HospitalId);

                if (hospital == null)
                {
                    TempData["ErrorMessage"] =
                        "Selected hospital was not found.";

                    return RedirectToAction(
                        nameof(CurrentRequestDetails),
                        new { id = request.RequestId });
                }

                // --------------------------------------------------------
                // Decrease hospital capacity
                // --------------------------------------------------------

                // Critical / Unconscious → ICU beds
                if (request.PatientCondition == PatientCondition.Critical ||
                    request.PatientCondition == PatientCondition.Unconscious)
                {
                    if (hospital.AvailableICUBeds <
                        request.NumberOfPatients)
                    {
                        TempData["ErrorMessage"] =
                            "The selected hospital does not have enough ICU beds.";

                        return RedirectToAction(
                            nameof(CurrentRequestDetails),
                            new { id = request.RequestId });
                    }

                    hospital.AvailableICUBeds -=
                        request.NumberOfPatients;
                }
                else
                {
                    // Stable / Serious → Normal beds
                    if (hospital.AvailableBeds <
                        request.NumberOfPatients)
                    {
                        TempData["ErrorMessage"] =
                            "The selected hospital does not have enough available beds.";

                        return RedirectToAction(
                            nameof(CurrentRequestDetails),
                            new { id = request.RequestId });
                    }

                    hospital.AvailableBeds -=
                        request.NumberOfPatients;
                }
            }

            // ------------------------------------------------------------
            // Restore hospital capacity when cancelling
            // ------------------------------------------------------------

            if (newStatus == RequestStatus.Cancelled &&
                request.Status == RequestStatus.GoingToHospital &&
                request.HospitalId != null)
            {
                var hospital = await _context.Hospitals
                    .FirstOrDefaultAsync(h =>
                        h.HospitalId == request.HospitalId);

                if (hospital != null)
                {
                    // Restore ICU beds
                    if (request.PatientCondition == PatientCondition.Critical ||
                        request.PatientCondition == PatientCondition.Unconscious)
                    {
                        hospital.AvailableICUBeds +=
                            request.NumberOfPatients;
                    }
                    else
                    {
                        // Restore normal beds
                        hospital.AvailableBeds +=
                            request.NumberOfPatients;
                    }
                }
            }

            // Update request status
            request.Status = newStatus;

            // ------------------------------------------------------------
            // Request completed/cancelled
            // Driver becomes available again
            // ------------------------------------------------------------

            if (newStatus == RequestStatus.Completed ||
                newStatus == RequestStatus.Cancelled)
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

            await _hubContext.Clients.All.SendAsync(
                "RequestStatusUpdated",
                request.RequestId,
                request.Status.ToString());

            TempData["SuccessMessage"] =
                newStatus == RequestStatus.Cancelled
                    ? "Request cancelled successfully."
                    : "Request status updated successfully.";

            // Completed or cancelled request returns to dashboard
            if (newStatus == RequestStatus.Completed ||
                newStatus == RequestStatus.Cancelled)
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
            return status == RequestStatus.Accepted ||
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
                newStatus == RequestStatus.Cancelled)
            {
                return true;
            }

            // OnTheWay -> Cancelled
            if (currentStatus == RequestStatus.OnTheWay &&
                newStatus == RequestStatus.Cancelled)
            {
                return true;
            }

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

        //This calculates the straight-line geographical distance between two
        //latitude/longitude coordinates using the Haversine formula.
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

        private async Task<List<AmbulanceRequest>> GetNearbyRequests(
            int driverId)
        {
            var ambulances = await _context.Ambulances
                .Where(a => a.DriverId == driverId)
                .ToListAsync();

            var allRequests = await _context.AmbulanceRequests
                .Include(r => r.User)
                .Where(r =>
                    r.DriverId == null &&
                    r.Status == RequestStatus.Requested)
                .OrderByDescending(r => r.RequestTime)
                .ToListAsync();

            var availableAmbulances = ambulances
                .Where(a =>
                    a.Status == "Available" &&
                    a.Latitude.HasValue &&
                    a.Longitude.HasValue)
                .ToList();

            return allRequests
                .Where(request =>
                    availableAmbulances.Any(ambulance =>
                        CalculateDistanceInKm(
                            ambulance.Latitude!.Value,
                            ambulance.Longitude!.Value,
                            request.PickupLatitude,
                            request.PickupLongitude) <= 5.0))
                .ToList();
        }


        // ============================================================
        // DRIVER - SELECT HOSPITAL
        // ============================================================

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectHospital(
            int id,
            int hospitalId)
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
                .FirstOrDefaultAsync(r =>
                    r.RequestId == id &&
                    r.DriverId == driver.DriverId);

            if (request == null)
            {
                return NotFound();
            }

            // Hospital can only be selected after patient pickup
            if (request.Status != RequestStatus.PatientPickedUp)
            {
                TempData["ErrorMessage"] =
                    "Hospital can only be selected after the patient is picked up.";

                return RedirectToAction(
                    nameof(CurrentRequestDetails),
                    new { id });
            }

            // Find selected hospital
            var hospital = await _context.Hospitals
    .FirstOrDefaultAsync(h =>
        h.HospitalId == hospitalId);

            if (hospital == null)
            {
                TempData["ErrorMessage"] =
                    "Selected hospital was not found.";

                return RedirectToAction(
                    nameof(CurrentRequestDetails),
                    new { id });
            }

            // Validate hospital suitability
            bool isSuitable = false;

            if (request.PatientCondition == PatientCondition.Critical ||
                request.PatientCondition == PatientCondition.Unconscious)
            {
                isSuitable =
                    hospital.EmergencyAvailable &&
                    hospital.AvailableICUBeds >= request.NumberOfPatients;
            }
            else if (request.PatientCondition == PatientCondition.Serious)
            {
                isSuitable =
                    hospital.EmergencyAvailable &&
                    hospital.AvailableBeds >= request.NumberOfPatients;
            }
            else if (request.PatientCondition == PatientCondition.Stable)
            {
                isSuitable =
                    hospital.AvailableBeds >= request.NumberOfPatients;
            }

            if (!isSuitable)
            {
                TempData["ErrorMessage"] =
                    "The selected hospital is not suitable for this patient.";

                return RedirectToAction(
                    nameof(CurrentRequestDetails),
                    new { id });
            }

            // Save selected hospital
            request.HospitalId = hospital.HospitalId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Hospital selected successfully.";

            return RedirectToAction(
                nameof(CurrentRequestDetails),
                new { id });
        }
    }
}

