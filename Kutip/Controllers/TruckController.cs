using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KutipSmart.Controllers
{
    [Authorize]
    public class TruckController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TruckController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Truck
        public async Task<IActionResult> Index()
        {
            var trucks = await _context.Trucks.ToListAsync();
            return View(trucks);
        }

        // GET: Truck/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var truck = await _context.Trucks.FirstOrDefaultAsync(t => t.TruckId == id);
            if (truck == null) return NotFound();

            return View(truck);
        }

        // GET: Truck/Create
        public IActionResult Create()
        {
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
            return View(truck);
        }

        // GET: Truck/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var truck = await _context.Trucks.FindAsync(id);
            if (truck == null) return NotFound();

            return View(truck);
        }

        // POST: Truck/Edit/5
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
            return View(truck);
        }

        // GET: Truck/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var truck = await _context.Trucks.FirstOrDefaultAsync(t => t.TruckId == id);
            if (truck == null) return NotFound();

            return View(truck);
        }

        // POST: Truck/Delete/5
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
    }
}
