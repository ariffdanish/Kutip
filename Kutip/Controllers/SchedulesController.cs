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
    [Authorize] // all actions require authentication
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        // GET: Schedules
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .ToListAsync();

            return View(schedules);
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        // GET: Schedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }

        [Authorize(Roles = "Admin")]
        // GET: Schedules/Create
        public IActionResult Create()
        {
            ViewBag.BinId = new SelectList(_context.Bin.ToList(), "BinId", "BinNo");
            ViewBag.TruckId = new SelectList(_context.Trucks.ToList(), "TruckId", "TruckNo");
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)));
            return View(new Schedule()); // send non-null model!
        }


        [Authorize(Roles = "Admin")]
        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                schedule.CreatedAt = DateTimeOffset.Now;
                schedule.UpdatedAt = DateTimeOffset.Now;
                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns on failed validation
            ViewBag.BinId = new SelectList(_context.Bin.ToList(), "BinId", "BinNo", schedule.BinId);
            ViewBag.TruckId = new SelectList(_context.Trucks.ToList(), "TruckId", "TruckNo", schedule.TruckId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);
            return View(schedule);
        }


        [Authorize(Roles = "Admin")]
        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            ViewData["BinId"] = new SelectList(_context.Bin, "BinId", "BinNo", schedule.BinId);
            ViewData["TruckId"] = new SelectList(_context.Trucks, "TruckId", "TruckNo", schedule.TruckId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);
            return View(schedule);
        }

        [Authorize(Roles = "Admin")]
        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Schedule schedule)
        {
            if (id != schedule.ScheduleId) return NotFound();

            if (ModelState.IsValid)
            {
                schedule.UpdatedAt = DateTimeOffset.Now;
                _context.Update(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BinId = new SelectList(_context.Bin, "BinId", "BinNo", schedule.BinId);
            ViewBag.TruckId = new SelectList(_context.Trucks, "TruckId", "TruckNo", schedule.TruckId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);

            return View(schedule);

            _context.Update(schedule);
            await _context.SaveChangesAsync();

            // Add notification
            _context.Notifications.Add(new Notification
            {
                Message = $"Schedule #{schedule.ScheduleId} updated at {DateTime.Now:f}"
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule updated successfully!";
            return RedirectToAction(nameof(Index));

        }


        [Authorize(Roles = "Admin")]
        // GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }


        [Authorize(Roles = "Admin")]
        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Truck)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                TempData["Error"] = "Schedule not found.";
                return RedirectToAction(nameof(Index));
            }

            // Optional: detach it from the truck (if required to avoid FK errors)
            if (schedule.Truck != null)
            {
                schedule.Truck.Schedules?.Remove(schedule);
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule deleted successfully!";
            return RedirectToAction(nameof(Index));

            _context.Notifications.Add(new Notification
            {
                Message = $"Schedule #{id} was deleted at {DateTime.Now:f}"
            });
            await _context.SaveChangesAsync();

        }


        [Authorize(Roles = "Admin")]
        // GET: Schedules/AutoSchedule
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

            const int KPI_LIMIT = 20;
            var availableTrucks = trucks
                .Where(t => t.Schedules.Count < KPI_LIMIT)
                .ToList();

            if (unscheduledBins.Any() && availableTrucks.Any())
            {
                ViewBag.CanSchedule = true;
                ViewBag.Message = "✅ Smart scheduling can be done.";
            }
            else
            {
                ViewBag.CanSchedule = false;
                ViewBag.Message = "⚠️ No bins or eligible trucks available for scheduling.";
            }

            ViewBag.BinCount = unscheduledBins.Count;
            ViewBag.TruckCount = availableTrucks.Count;

            return View();
        }


        [Authorize(Roles = "Admin")]
        // POST: Schedules/AutoScheduleConfirmed
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
                    s.ScheduledDateTime >= weekStart && s.ScheduledDateTime < weekEnd))
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            const int KPI_LIMIT = 20;
            const int MAX_WORK_DAYS = 3;

            var eligibleTrucks = trucks
                .Where(t => t.Schedules.Count < KPI_LIMIT)
                .ToList();

            if (!bins.Any() || !eligibleTrucks.Any())
            {
                TempData["Error"] = "No bins or eligible trucks available for scheduling.";
                return RedirectToAction(nameof(Index));
            }

            var allDays = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();
            var truckWorkDays = eligibleTrucks.ToDictionary(
                truck => truck.TruckId,
                truck => allDays.OrderBy(_ => Guid.NewGuid()).Take(MAX_WORK_DAYS).ToList()
            );

            int truckIndex = 0;
            foreach (var bin in bins)
            {
                var truck = eligibleTrucks[truckIndex % eligibleTrucks.Count];

                if (truck.Schedules.Count >= KPI_LIMIT || !truckWorkDays.ContainsKey(truck.TruckId))
                {
                    truckIndex++;
                    continue;
                }

                var assignDay = truckWorkDays[truck.TruckId]
                    .FirstOrDefault(d => !truck.Schedules.Any(s => s.ScheduledDateTime.Date == d.Date));

                if (assignDay == default)
                {
                    truckIndex++;
                    continue;
                }

                var schedule = new Schedule
                {
                    BinId = bin.BinId,
                    TruckId = truck.TruckId,
                    ScheduledDateTime = assignDay.AddHours(8),
                    Status = ScheduleStatus.Scheduled,
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now
                };

                _context.Schedules.Add(schedule);
                truck.Schedules.Add(schedule);
                truckIndex++;
            }

            // Save the new schedules
            await _context.SaveChangesAsync();

            // Log a system notification
            _context.Notifications.Add(new Notification
            {
                Message = "Smart auto-scheduling completed at " + DateTime.Now.ToString("f"),
                CreatedAt = DateTime.Now // ✅ Optional, but safer
            });

            await _context.SaveChangesAsync(); // Save the notification

            // Show success message to user
            TempData["Success"] = "Smart auto-scheduling completed!";
            return RedirectToAction(nameof(Index));


        }
    }
}
