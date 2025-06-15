using Kutip.Data;
using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
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

        public BinsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

        [Authorize(Roles = "Admin,TruckDriver")]
        public IActionResult Map()
        {
            var bins = _context.Bin.ToList();
            ViewBag.UserRole = User.IsInRole("Admin") ? "Admin" : "TruckDriver";
            return View(bins);
        }

        [Authorize(Roles = "Admin,TruckDriver")]
        [HttpPost]
        public async Task<IActionResult> ScanPlate([FromBody] ScanImageRequest request)
        {
            var base64Data = Regex.Match(request.ImageBase64, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            var imageBytes = Convert.FromBase64String(base64Data);

            var fileName = Guid.NewGuid() + ".png";
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            string detectedPlate;
            try
            {
                using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(filePath);
                var config = new Tesseract.PageSegMode[] { PageSegMode.SingleBlock };
                engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
                using var page = engine.Process(img, PageSegMode.SingleBlock);

                detectedPlate = page.GetText().Trim();
            }
            catch
            {
                return Json(new { success = false, message = "OCR processing failed." });
            }

            // Clean OCR result
            detectedPlate = detectedPlate.ToUpper().Trim();

            // Allow letters, numbers, hyphens, and remove noise
            detectedPlate = Regex.Replace(detectedPlate, @"[^A-Z0-9\-]", "");

            // Handle fallback
            if (string.IsNullOrWhiteSpace(detectedPlate))
            {
                return Json(new { success = false, detectedPlate = "N/A", message = "No readable text detected from image." });
            }


            var bin = _context.Bin.FirstOrDefault(b => b.BinNo == detectedPlate);
            if (bin != null && bin.Status == BinStatus.Active)
            {
                bin.Status = BinStatus.Collected; // Mark as collected
                bin.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    detectedPlate,
                    message = $"Bin '{detectedPlate}' is Active now."
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    detectedPlate,
                    message = "Bin not found or already Inactive (Collected)."
                });
            }
        }

        private bool BinExists(int id)
        {
            return _context.Bin.Any(e => e.BinId == id);
        }
    }

    public class ScanImageRequest
    {
        public string ImageBase64 { get; set; }
    }
}
