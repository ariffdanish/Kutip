using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Tesseract;

namespace Kutip.Controllers
{
    [Authorize]
    public class BinsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public BinsController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        public async Task<IActionResult> Index()
        {
            var bins = await _context.Bin
                .Include(b => b.Schedules)
                .ToListAsync();

            return View(bins);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Bin bin)
        {
            if (ModelState.IsValid)
            {
                bin.CreatedAt = DateTime.Now;
                bin.UpdatedAt = DateTime.Now;
                _context.Bin.Add(bin);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(bin);
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var bin = await _context.Bin.FirstOrDefaultAsync(b => b.BinId == id);
            if (bin == null)
                return NotFound();

            return View(bin);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var bin = await _context.Bin.FindAsync(id);
            if (bin == null)
                return NotFound();

            return View(bin);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bin bin)
        {
            if (id != bin.BinId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    bin.UpdatedAt = DateTime.Now;
                    _context.Update(bin);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BinExists(bin.BinId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(bin);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var bin = await _context.Bin.FirstOrDefaultAsync(m => m.BinId == id);
            if (bin == null)
                return NotFound();

            return View(bin);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bin = await _context.Bin.FindAsync(id);
            if (bin != null)
            {
                _context.Bin.Remove(bin);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Map()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userRole = User.IsInRole("Admin") ? "Admin" : "TruckDriver";
            ViewBag.UserRole = userRole;

            if (userRole == "Admin")
            {
                // Admin sees all bins with schedule info
                var binsWithSchedules = await _context.Bin
                    .Include(b => b.Schedules)
                        .ThenInclude(s => s.Truck)
                    .ToListAsync();

                return View(binsWithSchedules);
            }
            else
            {
                // Truck Driver: get only their assigned bins
                var driverName = $"{user.FirstName} {user.LastName}";

                var truck = await _context.Trucks
                    .Include(t => t.Schedules)
                        .ThenInclude(s => s.Bin)
                    .FirstOrDefaultAsync(t => t.DriverName == driverName);

                if (truck == null || !truck.Schedules.Any())
                {
                    return View(new List<Bin>());
                }
                var today = DateTime.Now.DayOfWeek;
                var scheduleDay = today switch
                {
                    DayOfWeek.Monday => ScheduleDay.Monday,
                    DayOfWeek.Tuesday => ScheduleDay.Tuesday,
                    DayOfWeek.Wednesday => ScheduleDay.Wednesday,
                    DayOfWeek.Thursday => ScheduleDay.Thursday,
                    DayOfWeek.Friday => ScheduleDay.Friday,
                    DayOfWeek.Saturday => ScheduleDay.Saturday,
                    DayOfWeek.Sunday => ScheduleDay.Sunday,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var todaysSchedules = truck.Schedules
                .Where(s => s.ScheduledDay == scheduleDay) // Compare with ScheduleDay enum
                .ToList();
                var assignedBins = todaysSchedules
                    .Select(s => s.Bin)
                    .Distinct()
                    .ToList();

                return View(assignedBins);
            }
        }

        private bool BinExists(int id)
        {
            return _context.Bin.Any(e => e.BinId == id);
        }

    }

}
