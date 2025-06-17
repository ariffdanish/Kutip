using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
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

            var schedules = await _context.Schedules
               .Where(s => s.TruckId == truck.TruckId)
               .Include(s => s.Bin)
               .Include(s => s.Truck)
               .OrderBy(s => s.ScheduledDate)
               .ToListAsync();

            return View("MySchedule",schedules);
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
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AutoSchedule()
        {
            var unscheduledBins = await _context.Bin
                .Where(b => !_context.Schedules.Any(s => s.BinId == b.BinId))
                .ToListAsync();

            var trucks = await _context.Trucks
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            ViewBag.BinCount = unscheduledBins.Count;
            ViewBag.TruckCount = trucks.Count;
            ViewBag.CanSchedule = unscheduledBins.Any() && trucks.Any();

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ViewSchedule()
        {
            // Get bins not assigned to any schedule
            var unscheduledBins = await _context.Bin
                .Where(b => !_context.Schedules.Any(s => s.BinId == b.BinId))
                .ToListAsync();

            // Get all active trucks
            var trucks = await _context.Trucks
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            // Fetch all scheduled items with related Bin and Truck data
            var schedules = await _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .ToListAsync();

            // Assign NotMapped property 'Street' based on the Bin's Street
            foreach (var schedule in schedules)
            {
                schedule.Street = schedule.Bin?.Street ?? "Unknown";
            }

            // ViewBag stats
            ViewBag.BinCount = unscheduledBins.Count;
            ViewBag.TruckCount = trucks.Count;
            ViewBag.CanSchedule = unscheduledBins.Any() && trucks.Any();

            return View(schedules);
        }



        private string NormalizeStreet(string street)
        {
            if (string.IsNullOrWhiteSpace(street)) return street;

            // Split into parts
            var parts = street.Split(' ', '-', '/', '\\');

            // Try to remove trailing number/letter at end (e.g., "1", "A", "Block B")
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (Regex.IsMatch(parts[i], @"^[a-zA-Z]$|^\d+$"))
                {
                    // Found a part that's only letters or digits at the end → likely an identifier
                    var normalized = string.Join(" ", parts.Take(i));
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        return normalized;
                    }
                }
            }

            return street;
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AutoScheduleConfirmed()
        {
            var bins = await _context.Bin.ToListAsync();
            var trucks = await _context.Trucks
                .Include(t => t.Schedules)
                .Where(t => t.Status == TruckStatus.Active)
                .ToListAsync();

            if (!trucks.Any())
            {
                TempData["Error"] = "No active trucks available.";
                return RedirectToAction(nameof(Index));
            }

            var assignedSchedules = new List<Schedule>();

            // Step 1: Define valid days (excluding Sunday)
            var allDays = Enum.GetValues(typeof(ScheduleDay))
                              .Cast<ScheduleDay>()
                              .Where(d => d != ScheduleDay.Sunday) // Exclude Sunday
                              .ToList();

            var random = new Random();

            // Step 2: Group bins by street similarity
            var binsByStreet = new Dictionary<string, List<Bin>>();

            foreach (var bin in bins)
            {
                var normalizedStreet = NormalizeStreet(bin.Street);

                if (!binsByStreet.ContainsKey(normalizedStreet))
                {
                    binsByStreet[normalizedStreet] = new List<Bin>();
                }

                binsByStreet[normalizedStreet].Add(bin);
            }

            var groupedBins = binsByStreet.Values.ToList(); // Each group = list of Bin objects with similar street names

            // Step 3: Assign groups fairly among trucks
            int totalGroups = groupedBins.Count;
            int truckCount = trucks.Count;

            int groupsPerTruck = totalGroups / truckCount;
            int extraGroups = totalGroups % truckCount;

            var truckBinAssignments = new Dictionary<Truck, List<List<Bin>>>();

            var shuffledTrucks = trucks.OrderBy(t => random.Next()).ToList();

            foreach (var truck in shuffledTrucks)
            {
                int groupCount = groupsPerTruck + (extraGroups > 0 ? 1 : 0);
                truckBinAssignments[truck] = new List<List<Bin>>();

                for (int i = 0; i < groupCount && groupedBins.Count > 0; i++)
                {
                    var selectedGroup = groupedBins[random.Next(groupedBins.Count)];
                    truckBinAssignments[truck].Add(selectedGroup);
                    groupedBins.Remove(selectedGroup);
                }

                if (extraGroups > 0) extraGroups--;
            }

            // Step 4: For each group, assign them to same truck and spaced-out days
            foreach (var truck in truckBinAssignments.Keys)
            {
                var groupsForTruck = truckBinAssignments[truck];

                foreach (var group in groupsForTruck)
                {
                    var selectedDays = new HashSet<ScheduleDay>();

                    while (selectedDays.Count < 3)
                    {
                        var candidateDay = allDays[random.Next(allDays.Count)];

                        bool hasGap = true;
                        foreach (var existingDay in selectedDays)
                        {
                            int existingIndex = allDays.IndexOf(existingDay);
                            int candidateIndex = allDays.IndexOf(candidateDay);

                            if (Math.Abs(existingIndex - candidateIndex) <= 1)
                            {
                                hasGap = false;
                                break;
                            }
                        }

                        if (hasGap)
                        {
                            selectedDays.Add(candidateDay);
                        }
                    }

                    foreach (var bin in group)
                    {
                        foreach (var day in selectedDays)
                        {
                            var schedule = new Schedule
                            {
                                BinId = bin.BinId,
                                TruckId = truck.TruckId,
                                ScheduledDay = day,
                                Status = ScheduleStatus.Scheduled,
                                CreatedAt = DateTimeOffset.Now,
                                UpdatedAt = DateTimeOffset.Now
                            };

                            assignedSchedules.Add(schedule);
                            truck.Schedules.Add(schedule);
                        }
                    }
                }
            }

            _context.Schedules.AddRange(assignedSchedules);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{assignedSchedules.Count} schedules created across {truckCount} trucks (Monday–Saturday only).";
            return RedirectToAction(nameof(Index));
        }


    }
}
