using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kutip.Controllers
{
    [Authorize]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchedulesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
        [Authorize(Roles = "TruckDriver")]
        public async Task<IActionResult> MySchedule()
        {
            // Get the currently logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // User not found or not logged in
            }

            // Extract the user's first and last name
            var driverFirstName = user.FirstName;
            var driverLastName = user.LastName;

            // Concatenate FirstName and LastName with a space in between
            var driverName = $"{driverFirstName} {driverLastName}";

            // Find the truck where DriverName matches
            var truck = await _context.Trucks
                .Include(t => t.Schedules)
                    .ThenInclude(s => s.Bin)
                .FirstOrDefaultAsync(t =>
                    t.DriverName == driverName);

            if (truck == null)
            {
                TempData["Error"] = "You are not assigned to any truck.";
                return View(new List<Schedule>());
            }

            // Get all schedules for this truck, ordered by date
            var schedules = truck.Schedules
                .OrderBy(s => s.ScheduledDate)
                .ToList();

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
            ViewBag.ScheduledDay = new SelectList(Enum.GetValues(typeof(ScheduleDay)));
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
                ViewBag.ScheduledDay = new SelectList(Enum.GetValues(typeof(ScheduleDay)), schedule.ScheduledDay);
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
            ViewBag.ScheduledDay = new SelectList(Enum.GetValues(typeof(ScheduleDay)), schedule.ScheduledDay);
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
                ViewBag.ScheduledDay = new SelectList(Enum.GetValues(typeof(ScheduleDay)), schedule.ScheduledDay);
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

        // POST: AutoScheduleConfirmed
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AutoScheduleConfirmed()
        {
            var bins = await _context.Bin.ToListAsync();
            var trucks = await _context.Trucks
                .Include(t => t.Schedules)
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            var assignedSchedules = new List<Schedule>();
            var days = Enum.GetValues(typeof(ScheduleDay)).Cast<ScheduleDay>().ToList();

            int dayIndex = 0;
            foreach (var bin in bins)
            {
                var truck = trucks.OrderBy(t => t.Schedules.Count).FirstOrDefault();
                if (truck == null) continue;

                var schedule = new Schedule
                {
                    BinId = bin.BinId,
                    TruckId = truck.TruckId,
                    ScheduledDay = days[dayIndex % days.Count],
                    Status = ScheduleStatus.Scheduled,
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now
                };

                assignedSchedules.Add(schedule);
                truck.Schedules.Add(schedule);
                dayIndex++;
            }

            _context.Schedules.AddRange(assignedSchedules);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{assignedSchedules.Count} bins scheduled.";
            return RedirectToAction(nameof(Index));
        }

<<<<<<< HEAD
        /*[Authorize(Roles = "TruckDriver")]
        public async Task<IActionResult> MySchedule()
        {
            var email = User.Identity?.Name;

            var truck = await _context.Trucks
                .FirstOrDefaultAsync(t => t.DriverName != null && t.DriverName.ToLower() == email.ToLower());

            if (truck == null)
            {
                TempData["Error"] = $"No truck assigned to you ({email}).";
                return RedirectToAction("Index", "Home");
            }

            // ✅ Pull all necessary data first into memory
            var rawSchedules = await _context.Schedules
                .Where(s => s.TruckId == truck.TruckId)
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .ToListAsync(); // Materialized BEFORE transformation

            // ✅ Then project into your view model safely with .ToString()
            var scheduleList = rawSchedules.Select(s => new TruckDriverScheduleViewModel
            {
                ScheduledDay = s.ScheduledDay.ToString(), // Safe: in-memory
                BinId = s.Bin.BinNo,
                Location = $"{s.Bin.Street}, {s.Bin.City}, {s.Bin.State} {s.Bin.PostCode}",
                Latitude = s.Bin.Latitude,
                Longitude = s.Bin.Longitude,
                TruckId = s.Truck.TruckNo
            }).ToList();

            TempData["DebugEmail"] = email;
            TempData["TruckDebug"] = truck.TruckNo;
            TempData["ScheduleCount"] = scheduleList.Count;

            return View("TruckDriverSchedule", scheduleList);
        }*/
=======
       
>>>>>>> 070e9ce5c696d01324bcf24cc8bd8a963eab93c4

    }
}
