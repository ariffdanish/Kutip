using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;

namespace Kutip.Controllers
{
    [Authorize]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchedulesController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDone(int scheduleId, string returnUrl)
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule != null)
            {
                // Update status to completed
                schedule.Status = ScheduleStatus.Completed;
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
            }

            // Redirect back to the same page
            return RedirectToAction("MyBin", "Bins");
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

            return View("MySchedule", schedules);
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
                ViewBag.ScheduledDay = new SelectList(Enum.GetValues(typeof(ScheduleDay)), schedule.ScheduledDay);
                return View(schedule);
            }
            schedule.Status = ScheduleStatus.Scheduled;
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
            // Get unscheduled bins only
            var scheduledBinIds = await _context.Schedules
                .Select(s => s.BinId)
                .Distinct()
                .ToListAsync();

            var unscheduledBins = await _context.Bin
                .Where(b => !_context.Schedules.Any(s => s.BinId == b.BinId))
                .ToListAsync();

            if (!unscheduledBins.Any())
            {
                TempData["Info"] = "No new bins found. All bins are already scheduled.";
                return RedirectToAction(nameof(Index));
            }

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
            var allDays = Enum.GetValues(typeof(ScheduleDay))
                              .Cast<ScheduleDay>()
                              .Where(d => d != ScheduleDay.Sunday) // Exclude Sunday
                              .ToList();

            var random = new Random();

            // Step 1: Group bins by street similarity
            var binsByStreet = new Dictionary<string, List<Bin>>();

            foreach (var bin in unscheduledBins)
            {
                var normalizedStreet = NormalizeStreet(bin.Street);

                if (!binsByStreet.ContainsKey(normalizedStreet))
                {
                    binsByStreet[normalizedStreet] = new List<Bin>();
                }

                binsByStreet[normalizedStreet].Add(bin);
            }

            var groupedBins = binsByStreet.Values.ToList(); // Each group = list of Bin objects with similar street names

            // Step 2: Assign groups fairly among trucks
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

            // Step 3: For each group, assign them to same truck and spaced-out days
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

            if (assignedSchedules.Any())
            {
                _context.Schedules.AddRange(assignedSchedules);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{assignedSchedules.Count} schedules created for {unscheduledBins.Count} new bins (Monday–Saturday only).";
            }
            else
            {
                TempData["Info"] = "No new bins could be scheduled.";
            }

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteSchedule()
        {
            try
            {
                var allSchedules = _context.Schedules.ToList();
                _context.Schedules.RemoveRange(allSchedules);
                _context.SaveChanges();

                TempData["Success"] = "All schedules have been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting schedules.";
                // Log exception (ex) here if using logging framework
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "TruckDriver")]
        [HttpPost]
        public async Task<IActionResult> ScanPlate([FromBody] ScanImageRequest request)
        {
            // Step 1: Decode Base64 and save image
            var base64Data = Regex.Match(request.ImageBase64, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            var imageBytes = Convert.FromBase64String(base64Data);
            var fileName = Guid.NewGuid() + ".png";
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            // Step 2: OCR using Tesseract
            string detectedPlate;
            try
            {
                string pythonScriptPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "detect_and_ocr.py"));
                string yolov8ModelPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "yolov8-license-plates.pt"));

                // Arguments for Python script
                string arguments = $"\"{filePath}\" \"{yolov8ModelPath}\"";

                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = $"{pythonScriptPath} {arguments}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process() { StartInfo = processStartInfo };
                process.Start();

                detectedPlate = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "OCR processing failed." });
            }

            // Step 3: Clean up OCR output

            // Extract only the part starting with BIN and up to 8 characters max (or as needed)
            var match = Regex.Match(detectedPlate, @"BIN[A-Z0-9]{1,8}");

            if (match.Success)
            {
                detectedPlate = match.Value.Replace('O', '0').Replace('o', '0');
            }
            else
            {
                detectedPlate = string.Empty; // or keep original if fallback is needed
            }

            if (string.IsNullOrWhiteSpace(detectedPlate))
            {
                return Json(new { success = false, detectedPlate = "N/A", message = "No readable text detected from image." });
            }

            // Step 4: Get current user and truck
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var driverName = $"{currentUser.FirstName} {currentUser.LastName}";

            var truck = await _context.Trucks
                .Include(t => t.Schedules)
                    .ThenInclude(s => s.Bin)
                .FirstOrDefaultAsync(t => t.DriverName == driverName && t.Status == TruckStatus.Active);

            if (truck == null)
            {
                return Json(new { success = false, message = "You are not assigned to any active truck." });
            }

            // Step 5: Find matching Bin based on plate
            var bin = await _context.Bin.FirstOrDefaultAsync(b => b.BinNo == detectedPlate);

            if (bin == null)
            {
                return Json(new
                {
                    success = false,
                    detectedPlate,
                    message = "No bin found with this plate number."
                });
            }

            // Step 6: Find today's schedule for this bin and truck
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


            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s =>
                    s.BinId == bin.BinId &&
                    s.TruckId == truck.TruckId &&
                    s.ScheduledDay == scheduleDay);

            if (schedule == null)
            {
                return Json(new
                {
                    success = false,
                    detectedPlate,
                    message = "This bin is not scheduled for pickup today for your truck."
                });
            }

            if (schedule.Status == ScheduleStatus.Completed)
            {
                return Json(new
                {
                    success = false,
                    detectedPlate,
                    message = "This bin has already been completed."
                });
            }

            // Step 7: Update both Schedule and Bin statuses
            schedule.Status = ScheduleStatus.Completed;
            schedule.UpdatedAt = DateTime.Now;

            _context.Update(schedule);


            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                detectedPlate,
                message = "Schedule and bin marked as completed."
            });
        }

        public class ScanImageRequest
        {
            public string ImageBase64 { get; set; }
        }
    }
}
