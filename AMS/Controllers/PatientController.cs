using AMS.Data;
using AMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AMS.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var recentRequests = await _context.AmbulanceRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestTime)
                .Take(3)
                .ToListAsync();

            var model = new PatientDashboardViewModel
            {
                FullName = user.FullName,
                RecentRequests = recentRequests
            };

            return View(model);
        }
    }
}