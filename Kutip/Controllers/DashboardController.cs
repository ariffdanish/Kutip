using Microsoft.AspNetCore.Mvc;
using Kutip.Models;
using System.Linq;
using System.Collections.Generic;
using Kutip.Data;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using Rotativa.AspNetCore;

namespace Kutip.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(
             string cityFilter,
             string stateFilter,
             BinStatus? binStatusFilter,
             string truckNoFilter,
             TruckStatus? truckStatusFilter
)
        {
            var viewModel = new DashboardViewModel();

            // Build Bin Query
            var binsQuery = _context.Bin.Include(b => b.Schedules).AsQueryable();
            if (!string.IsNullOrEmpty(cityFilter)) binsQuery = binsQuery.Where(b => b.City == cityFilter);
            if (!string.IsNullOrEmpty(stateFilter)) binsQuery = binsQuery.Where(b => b.State == stateFilter);
            if (binStatusFilter.HasValue) binsQuery = binsQuery.Where(b => b.Status == binStatusFilter.Value);

            var bins = binsQuery.ToList(); // Materialize once

            // Build Truck Query
            var trucksQuery = _context.Trucks.AsQueryable();
            if (!string.IsNullOrEmpty(truckNoFilter)) trucksQuery = trucksQuery.Where(t => t.TruckNo.Contains(truckNoFilter));
            if (truckStatusFilter.HasValue) trucksQuery = trucksQuery.Where(t => t.Status == truckStatusFilter.Value);

            var trucks = trucksQuery.ToList(); // Materialize once

            //Get today truck assign
            var today = DateTime.Today;
            viewModel.TrucksAssignedToday = _context.Schedules
                .AsEnumerable()
                .Where(s => s.ScheduledDay == (ScheduleDay)DateTime.Today.DayOfWeek)
                .Select(s => s.TruckId)
                .Distinct()
                .Count();

            // Inject filtered lists into ViewModel
            viewModel.Bins = bins;
            viewModel.Trucks = trucks;

            // Get latest schedule for each bin
            var scheduleLookup = bins.ToDictionary(
                bin => bin.BinId,
                bin => bin.Schedules
                    .OrderByDescending(s => s.ScheduledDay)
                    .FirstOrDefault()
            );
            viewModel.ScheduleLookup = scheduleLookup;

            // Get today's date
            viewModel.BinsScheduledToday = bins
                 .Select(bin => new
                 {
                     Bin = bin,
                     TodaysSchedule = bin.Schedules
                        .Where(s => s.UpdatedAt.Date == today.Date &&
                                     (s.Status == ScheduleStatus.Completed ||
                                      s.Status == ScheduleStatus.Missed))
                        .OrderByDescending(s => s.UpdatedAt)
                        .FirstOrDefault()
                  })
                  .Where(x => x.TodaysSchedule != null)
                  .Select(x => x.Bin)
                  .ToList();


            // Populate dropdown data
            viewModel.AllCities = _context.Bin.Select(b => b.City).Distinct().ToList();
            viewModel.AllStates = _context.Bin.Select(b => b.State).Distinct().ToList();
            viewModel.AllBinStatuses = Enum.GetValues(typeof(BinStatus)).Cast<BinStatus>().ToList();
            viewModel.AllTruckStatuses = Enum.GetValues(typeof(TruckStatus)).Cast<TruckStatus>().ToList();
            viewModel.AllTruckNos = _context.Trucks.Select(t => t.TruckNo).Distinct().ToList();

            // Set selected filters
            viewModel.SelectedCity = cityFilter;
            viewModel.SelectedState = stateFilter;
            viewModel.SelectedBinStatus = binStatusFilter;
            viewModel.SelectedTruckNo = truckNoFilter;
            viewModel.SelectedTruckStatus = truckStatusFilter;

            return View(viewModel);
        }

        //////////////////////////////////////////////////////////////
        /// REPORTING


        ///Truck
        public IActionResult TruckReportPreviewWrapper(TruckStatus? truckStatusFilter)
        {
            var trucks = _context.Trucks.AsQueryable();

            if (truckStatusFilter.HasValue)
            {
                trucks = trucks.Where(t => t.Status == truckStatusFilter.Value);
            }

            ViewBag.SelectedTruckStatus = truckStatusFilter;
            ViewBag.AllTruckStatuses = Enum.GetValues(typeof(TruckStatus)).Cast<TruckStatus>().ToList();

            return View("TruckReport", trucks.ToList());
        }

        public IActionResult ExportTruckPDF(TruckStatus? truckStatusFilter)
        {
            var trucks = _context.Trucks.AsQueryable();

            if (truckStatusFilter.HasValue)
            {
                trucks = trucks.Where(t => t.Status == truckStatusFilter.Value);
            }

            return new ViewAsPdf("TruckReport", trucks.ToList())
            {
                FileName = "TruckReport.pdf"
            };
        }

        public IActionResult PreviewTruckReport()
        {
            return RedirectToAction("TruckReportPreviewWrapper", new { preview = true });
        }
        ///END TRUCK

        ///BIN
        public IActionResult BinReportPreviewWrapper(BinStatus? binStatusFilter, string preview)
        {
            var bins = _context.Bin.Include(b => b.Schedules).AsQueryable();

            if (binStatusFilter.HasValue)
            {
                bins = bins.Where(b => b.Status == binStatusFilter.Value);
            }

            ViewBag.SelectedBinStatus = binStatusFilter;
            ViewBag.AllBinStatuses = Enum.GetValues(typeof(BinStatus)).Cast<BinStatus>().ToList();
            ViewBag.IsPreview = string.Equals(preview, "true", StringComparison.OrdinalIgnoreCase);

            return View("BinReport", bins.ToList());
        }


        public IActionResult ExportBinPDF(BinStatus? binStatusFilter)
        {
            var bins = _context.Bin.Include(b => b.Schedules).AsQueryable();

            if (binStatusFilter.HasValue)
            {
                bins = bins.Where(b => b.Status == binStatusFilter.Value);
            }

            return new ViewAsPdf("BinReport", bins.ToList())
            {
                FileName = "BinReport.pdf"
            };
        }

        public IActionResult PreviewBinReport()
        {
            return RedirectToAction("BinReportPreviewWrapper", new { preview = true });
        }

        ///END BIN

        ///PICKUP
        public IActionResult PickupReportPreview(DateTime? startDate, DateTime? endDate, string streetFilter = null, string preview = "false")
        {
            var query = _context.PickupEvents
                .Include(e => e.Bin)
                .Include(e => e.Truck)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.EventRecordedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EventRecordedAt <= endDate.Value);

            // ✅ Use partial match for street name
            if (!string.IsNullOrEmpty(streetFilter))
                query = query.Where(e => EF.Functions.Like(e.Bin.Street, $"%{streetFilter}%"));

            var results = query.ToList();

            ViewBag.IsPreview = string.Equals(preview, "true", StringComparison.OrdinalIgnoreCase);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStreet = streetFilter;

            return View("PickupReport", results);
        }

        public IActionResult ExportPickupReport(DateTime? startDate, DateTime? endDate, string streetFilter = null)
        {
            var query = _context.PickupEvents
                .Include(e => e.Bin)
                .Include(e => e.Truck)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.EventRecordedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EventRecordedAt <= endDate.Value);

            if (!string.IsNullOrEmpty(streetFilter))
                query = query.Where(e => EF.Functions.Like(e.Bin.Street, $"%{streetFilter}%"));

            var results = query.ToList();

            return new ViewAsPdf("PickupReport", results)
            {
                FileName = "PickupReport.pdf"
            };
        }

        ///END PICKUP


    }
}
