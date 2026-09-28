using AMS.Data;
using AMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Controllers
{
    public class HospitalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HospitalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Hospital
        public async Task<IActionResult> Index()
        {
            var hospitals = await _context.Hospitals
                .ToListAsync();

            return View(hospitals);
        }

        // GET: Hospital/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hospital = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.HospitalId == id);

            if (hospital == null)
            {
                return NotFound();
            }

            return View(hospital);
        }

        // GET: Hospital/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Hospital/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Hospital hospital)
        {
            if (ModelState.IsValid)
            {
                _context.Hospitals.Add(hospital);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(hospital);
        }

        // GET: Hospital/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hospital = await _context.Hospitals
                .FindAsync(id);

            if (hospital == null)
            {
                return NotFound();
            }

            return View(hospital);
        }

        // POST: Hospital/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Hospital hospital)
        {
            if (id != hospital.HospitalId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Hospitals.Update(hospital);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(hospital);
        }

        // GET: Hospital/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hospital = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.HospitalId == id);

            if (hospital == null)
            {
                return NotFound();
            }

            return View(hospital);
        }

        // POST: Hospital/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hospital = await _context.Hospitals
                .FindAsync(id);

            if (hospital != null)
            {
                _context.Hospitals.Remove(hospital);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}