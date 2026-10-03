using AMS.Data;
using AMS.Models;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AmbulanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AmbulanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ambulance
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var ambulances = await _context.Ambulances
                .Include(a => a.Driver)
                .OrderBy(a => a.VehicleNumber)
                .ToListAsync();

            return View(ambulances);
        }

        // GET: Ambulance/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ambulance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ambulance model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Status = "Available";

            _context.Ambulances.Add(model);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Ambulance/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a => a.AmbulanceId == id);

            if (ambulance == null)
            {
                return NotFound();
            }

            return View(ambulance);
        }

        // POST: Ambulance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Ambulance model)
        {
            if (id != model.AmbulanceId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a => a.AmbulanceId == id);

            if (ambulance == null)
            {
                return NotFound();
            }

            ambulance.VehicleNumber = model.VehicleNumber;
            ambulance.Latitude = model.Latitude;
            ambulance.Longitude = model.Longitude;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Ambulance/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var ambulance = await _context.Ambulances
                .Include(a => a.Driver)
                .FirstOrDefaultAsync(a => a.AmbulanceId == id);

            if (ambulance == null)
            {
                return NotFound();
            }

            return View(ambulance);
        }

        // POST: Ambulance/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a => a.AmbulanceId == id);

            if (ambulance == null)
            {
                return NotFound();
            }

            if (ambulance.DriverId != null)
            {
                TempData["ErrorMessage"] =
                    "This ambulance is assigned to a driver and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            _context.Ambulances.Remove(ambulance);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Ambulance/Assign/5
        [HttpGet]
        public async Task<IActionResult> Assign(int id)
        {
            var ambulance = await _context.Ambulances
                .Include(a => a.Driver)
                .FirstOrDefaultAsync(a => a.AmbulanceId == id);

            if (ambulance == null)
            {
                return NotFound();
            }

            var drivers = await _context.Drivers
                .OrderBy(d => d.FullName)
                .ToListAsync();

            ViewBag.Drivers = drivers;

            var model = new AmbulanceAssignmentViewModel
            {
                AmbulanceId = ambulance.AmbulanceId,
                VehicleNumber = ambulance.VehicleNumber,
                DriverId = ambulance.DriverId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AmbulanceAssignmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var drivers = await _context.Drivers
                    .OrderBy(d => d.FullName)
                    .ToListAsync();

                ViewBag.Drivers = drivers;
                return View(model);
            }

            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a => a.AmbulanceId == model.AmbulanceId);

            if (ambulance == null)
                return NotFound();

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.DriverId == model.DriverId);

            if (driver == null)
            {
                ModelState.AddModelError(
                    "DriverId",
                    "Selected driver was not found.");

                var drivers = await _context.Drivers
                    .OrderBy(d => d.FullName)
                    .ToListAsync();

                ViewBag.Drivers = drivers;
                return View(model);
            }

            ambulance.DriverId = driver.DriverId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(int id)
        {
            var ambulance = await _context.Ambulances
                .FirstOrDefaultAsync(a => a.AmbulanceId == id);

            if (ambulance == null)
                return NotFound();

            ambulance.DriverId = null;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}