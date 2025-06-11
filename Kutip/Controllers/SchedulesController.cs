using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kutip.Controllers
{
    [Authorize]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .ToListAsync();
            return View(schedules);
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            return schedule == null ? NotFound() : View(schedule);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.BinId = new SelectList(_context.Bin.ToList(), "BinId", "BinNo");
            ViewBag.TruckId = new SelectList(_context.Trucks.ToList(), "TruckId", "TruckNo");
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)));
            return View(new Schedule());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BinId = new SelectList(_context.Bin.ToList(), "BinId", "BinNo", schedule.BinId);
                ViewBag.TruckId = new SelectList(_context.Trucks.ToList(), "TruckId", "TruckNo", schedule.TruckId);
                ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);
                return View(schedule);
            }

            schedule.CreatedAt = DateTimeOffset.Now;
            schedule.UpdatedAt = DateTimeOffset.Now;

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            ViewBag.BinId = new SelectList(_context.Bin.ToList(), "BinId", "BinNo", schedule.BinId);
            ViewBag.TruckId = new SelectList(_context.Trucks.ToList(), "TruckId", "TruckNo", schedule.TruckId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);
            return View(schedule);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Schedule schedule)
        {
            if (id != schedule.ScheduleId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.BinId = new SelectList(_context.Bin, "BinId", "BinNo", schedule.BinId);
                ViewBag.TruckId = new SelectList(_context.Trucks, "TruckId", "TruckNo", schedule.TruckId);
                ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);
                return View(schedule);
            }

            schedule.UpdatedAt = DateTimeOffset.Now;
            _context.Update(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            return schedule == null ? NotFound() : View(schedule);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                TempData["Error"] = "Schedule not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AutoSchedule()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var unscheduledBins = await _context.Bin
                .Where(b => !_context.Schedules.Any(s =>
                    s.BinId == b.BinId &&
                    s.ScheduledDateTime >= weekStart &&
                    s.ScheduledDateTime < weekEnd))
                .ToListAsync();

            var trucks = await _context.Trucks
                .Include(t => t.Schedules.Where(s =>
                    s.ScheduledDateTime >= weekStart &&
                    s.ScheduledDateTime < weekEnd))
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            const int KPI_LIMIT = 3;
            var availableTrucks = trucks.Where(t => t.Schedules.Count < KPI_LIMIT).ToList();

            ViewBag.CanSchedule = unscheduledBins.Any() && availableTrucks.Any();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AutoScheduleConfirmed()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var bins = await _context.Bin
                .Where(b => !_context.Schedules
                    .Any(s => s.BinId == b.BinId &&
                              s.ScheduledDateTime >= weekStart &&
                              s.ScheduledDateTime < weekEnd))
                .ToListAsync();

            var trucks = await _context.Trucks
                .Include(t => t.Schedules.Where(s =>
                    s.ScheduledDateTime >= weekStart &&
                    s.ScheduledDateTime < weekEnd))
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            var assignedSchedules = new List<Schedule>();

            foreach (var bin in bins)
            {
                var nearestTruck = trucks
                    .OrderBy(t => GetDistance(1.5584, 103.6371, bin.Latitude, bin.Longitude))
                    .FirstOrDefault();

                if (nearestTruck == null || nearestTruck.Schedules.Count >= 3)
                    continue;

                var availableDay = Enumerable.Range(0, 7)
                    .Select(i => weekStart.AddDays(i))
                    .FirstOrDefault(d => !nearestTruck.Schedules.Any(s => s.ScheduledDateTime.Date == d.Date));

                if (availableDay == default) continue;

                var schedule = new Schedule
                {
                    BinId = bin.BinId,
                    TruckId = nearestTruck.TruckId,
                    ScheduledDateTime = availableDay.AddHours(8),
                    Status = ScheduleStatus.Scheduled,
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now
                };

                assignedSchedules.Add(schedule);
                nearestTruck.Schedules.Add(schedule);
            }

            _context.Schedules.AddRange(assignedSchedules);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{assignedSchedules.Count} bins auto-scheduled.";
            return RedirectToAction(nameof(Index));
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371;
            double dLat = Math.PI / 180 * (lat2 - lat1);
            double dLon = Math.PI / 180 * (lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(Math.PI / 180 * lat1) * Math.Cos(Math.PI / 180 * lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
