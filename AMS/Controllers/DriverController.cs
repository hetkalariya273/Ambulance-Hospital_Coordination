using AMS.Data;
using AMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Controllers
{
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Driver
        public IActionResult Index()
        {
            var drivers = _context.Drivers.ToList();

            return View(drivers);
        }

        //GET : Driver/Details/5

        public async Task<IActionResult> Details(int? id)
        {
            if(id == null)
            {
                return NotFound();


            }

            var driver = await _context.Drivers.FirstOrDefaultAsync(m => m.DriverId == id);

            if(driver == null)
                return NotFound();

            return View(driver);
        }

        //GET : Driver/Create
        public IActionResult Create()
        {
            return View();
        }

        //POST : Driver/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Driver driver)
        {
            if(ModelState.IsValid)
            {
                _context.Add(driver);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(driver);
        }

        //GET : Driver/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers.FindAsync(id);

            if (driver == null)
                return NotFound();

            return View(driver);
        }

        //POST : Driver/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id ,Driver driver)
        {
            if (id == null)
            {
                return NotFound();
            }

            if(ModelState.IsValid)
            {
                _context.Update(driver);
                await _context.SaveChangesAsync();
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

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == id);

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
    }
}