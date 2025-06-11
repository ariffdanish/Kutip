using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
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

            if (schedule == null) return NotFound();

            return View(schedule);
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
            if (ModelState.IsValid)
            {
                schedule.CreatedAt = DateTimeOffset.Now;
                schedule.UpdatedAt = DateTimeOffset.Now;

                var truck = await _context.Trucks.FindAsync(schedule.TruckId);

                // ✅ Only assign DriverName if it's not already set
                if (truck != null && string.IsNullOrEmpty(truck.DriverName))
                {
                    // Get the first available TruckDriver
                    var driverEmail = await (from user in _context.Users
                                             join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                             join role in _context.Roles on userRole.RoleId equals role.Id
                                             where role.Name == "TruckDriver"
                                             select user.Email).FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(driverEmail))
                    {
                        truck.DriverName = driverEmail;
                        _context.Trucks.Update(truck); // ✅ Persist the driver assignment
                    }
                }

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync(); // ✅ Save both truck + schedule
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BinId = new SelectList(_context.Bin.ToList(), "BinId", "BinNo", schedule.BinId);
            ViewBag.TruckId = new SelectList(_context.Trucks.ToList(), "TruckId", "TruckNo", schedule.TruckId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);
            return View(schedule);
        }





        [Authorize(Roles = "Admin")]
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

                _context.Notifications.Add(new Notification
                {
                    Message = $"Schedule #{schedule.ScheduleId} updated at {DateTime.Now:f}"
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Schedule updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BinId = new SelectList(_context.Bin, "BinId", "BinNo", schedule.BinId);
            ViewBag.TruckId = new SelectList(_context.Trucks, "TruckId", "TruckNo", schedule.TruckId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ScheduleStatus)), schedule.Status);

            return View(schedule);
        }

        [Authorize(Roles = "Admin")]
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

            if (schedule.Truck != null)
            {
                schedule.Truck.Schedules?.Remove(schedule);
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule deleted successfully!";

            _context.Notifications.Add(new Notification
            {
                Message = $"Schedule #{id} was deleted at {DateTime.Now:f}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

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

            const int KPI_LIMIT = 20;
            var availableTrucks = trucks
                .Where(t => t.Schedules.Count < KPI_LIMIT)
                .ToList();

            ViewBag.CanSchedule = unscheduledBins.Any() && availableTrucks.Any();
            ViewBag.Message = ViewBag.CanSchedule ? "✅ Smart scheduling can be done." : "⚠️ No bins or eligible trucks available for scheduling.";
            ViewBag.BinCount = unscheduledBins.Count;
            ViewBag.TruckCount = availableTrucks.Count;

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

            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification
            {
                Message = $"Auto scheduling done for week starting {weekStart:d}.",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Auto scheduling completed successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Mark as Completed
        [Authorize(Roles = "Admin,TruckDriver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDone(int scheduleId)
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null) return NotFound();

            if (schedule.Status == ScheduleStatus.Scheduled)
            {
                schedule.Status = ScheduleStatus.Completed;
                schedule.UpdatedAt = DateTimeOffset.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Schedule #{scheduleId} marked as Completed.";
            }
            else
            {
                TempData["Error"] = "Cannot mark schedule as Done because it is not in Scheduled status.";
            }

            return RedirectToAction("Index", "Bins");

        }

        // ❌ Mark as Missed
        [Authorize(Roles = "Admin,TruckDriver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportIssue(int scheduleId, string issueType)
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null) return NotFound();

            if (schedule.Status == ScheduleStatus.Scheduled)
            {
                schedule.Status = ScheduleStatus.Missed;
                schedule.UpdatedAt = DateTimeOffset.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Schedule #{scheduleId} marked as Missed due to issue: {issueType}";
            }
            else
            {
                TempData["Error"] = "Cannot report issue because schedule is not in Scheduled status.";
            }

            return RedirectToAction("Index", "Bins");

        }

        [Authorize(Roles = "TruckDriver")]
        public async Task<IActionResult> MySchedule()
        {
            var email = User.Identity.Name;
            TempData["DebugEmail"] = email;


            var truck = await _context.Trucks
                .FirstOrDefaultAsync(t => t.DriverName != null && t.DriverName.ToLower() == email.ToLower());

            if (truck == null)
            {
                TempData["Error"] = $"No truck assigned to you ({email}).";
                return RedirectToAction("Index", "Home");
            }

            var schedule = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .Where(s => s.TruckId == truck.TruckId)
                .Select(s => new
                {
                    s.ScheduledDateTime,
                    BinId = s.Bin.BinNo,
                    Location = s.Bin.Street + ", " + s.Bin.City + ", " + s.Bin.State + " " + s.Bin.PostCode,
                    s.Bin.Latitude,
                    s.Bin.Longitude,
                    TruckPlate = s.Truck.TruckNo
                })
                .ToListAsync();

            ViewBag.Schedule = schedule;
            TempData["DebugEmail"] = email;
            TempData["TruckDebug"] = truck?.TruckNo ?? "none";

            var schedules = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .Where(s => s.TruckId == truck.TruckId)
                .Select(s => new
                {
                    s.ScheduledDateTime,
                    BinId = s.Bin.BinNo,
                    Location = s.Bin.Street + ", " + s.Bin.City + ", " + s.Bin.State + " " + s.Bin.PostCode,
                    s.Bin.Latitude,
                    s.Bin.Longitude,
                    TruckPlate = s.Truck.TruckNo
                })
                .ToListAsync();

            TempData["ScheduleCount"] = schedule.Count;

            ViewBag.Schedule = schedule;
            return View("TruckDriverSchedule");
        }

    }
}
