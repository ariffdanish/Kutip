using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Kutip.Controllers
{
    [Authorize]
    public class BinsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BinsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        public IActionResult Index()
        {
            List<Bin> bins = _context.Bin.ToList();
            return View(bins);
        }

        [Authorize(Roles = "Admin")]
        // GET: Bin/Create
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        // POST: Bin/Create
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
        // GET: Bin/Edit/5
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
        // POST: Bin/Edit/5
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

        private bool BinExists(int id)
        {
            return _context.Bin.Any(e => e.BinId == id);
        }

        // Controller: BinController.cs

        [Authorize(Roles = "Admin")]
        // GET: Bin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var bin = await _context.Bin
                .FirstOrDefaultAsync(m => m.BinId == id);
            if (bin == null)
                return NotFound();

            return View(bin);
        }

        [Authorize(Roles = "Admin")]
        // POST: Bin/Delete/5
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

        [Authorize(Roles = "Admin")]
        // GET: Bins/Map
        public IActionResult Map()
        {
            List<Bin> bins = _context.Bin.ToList();
            return View(bins);
        }


    }
}