using Kutip.Models;
using Kutip.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace Kutip.Controllers
{
    [Authorize]
    public class TruckController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TruckController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var trucks = _context.Trucks.ToList();
            return View(trucks);
        }

        // GET: Truck/Create
        public async Task<IActionResult> Create()
        {
            await LoadTruckDriversAsync();
            return View();
        }

        // POST: Truck/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Truck truck)
        {
            if (ModelState.IsValid)
            {
                truck.CreatedAt = DateTimeOffset.UtcNow;
                truck.UpdatedAt = DateTimeOffset.UtcNow;

                _context.Trucks.Add(truck);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await LoadTruckDriversAsync();
            return View(truck);
        }

        // GET: Truck/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var truck = await _context.Trucks.FindAsync(id);
            if (truck == null) return NotFound();

            await LoadTruckDriversAsync();
            return View(truck);
        }

        // POST: Truck/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Truck truck)
        {
            if (id != truck.TruckId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    truck.UpdatedAt = DateTimeOffset.UtcNow;
                    _context.Update(truck);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Trucks.AnyAsync(t => t.TruckId == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await LoadTruckDriversAsync();
            return View(truck);
        }

        // GET: Truck/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var truck = await _context.Trucks.FirstOrDefaultAsync(t => t.TruckId == id);
            if (truck == null) return NotFound();

            return View(truck);
        }

        // GET: Truck/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var truck = await _context.Trucks.FirstOrDefaultAsync(t => t.TruckId == id);
            if (truck == null) return NotFound();

            return View(truck);
        }

        // POST: Truck/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var truck = await _context.Trucks.FindAsync(id);
            if (truck != null)
            {
                _context.Trucks.Remove(truck);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Truck/Map2
        public IActionResult Map2()
        {
            var trucks = _context.Trucks
                .Include(t => t.Schedules)
                    .ThenInclude(s => s.Bin)
                .Where(t => t.Status == TruckStatus.Active)
                .ToList();

            return View(trucks);
        }

        // Shared helper to load drivers into ViewBag
        private async Task LoadTruckDriversAsync()
        {
            var truckDrivers = await _userManager.GetUsersInRoleAsync("TruckDriver");
            var validNamePattern = new Regex(@"^[A-Za-z\s'-]+$");

            var driverList = truckDrivers
                .Where(u =>
                    !string.IsNullOrWhiteSpace(u.FirstName) &&
                    !string.IsNullOrWhiteSpace(u.LastName) &&
                    validNamePattern.IsMatch($"{u.FirstName} {u.LastName}")
                )
                .Select(u => new SelectListItem
                {
                    Value = $"{u.FirstName} {u.LastName}",
                    Text = $"{u.FirstName} {u.LastName}"
                })
                .ToList();

            if (!driverList.Any())
            {
                driverList.Add(new SelectListItem
                {
                    Value = "",
                    Text = "No Truck Drivers Available",
                    Disabled = true
                });
            }

            ViewBag.TruckDrivers = driverList;
        }
    }
}
