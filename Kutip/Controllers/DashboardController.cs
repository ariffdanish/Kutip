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

            // Get bins with any schedule scheduled for today

            viewModel.BinsScheduledToday = bins
                .Where(bin =>
                {
                    var todaysSchedules = bin.Schedules
                        .Where(s => s.ScheduledDay == (ScheduleDay)DateTime.Today.DayOfWeek)
                        .ToList();

                    return todaysSchedules != null && todaysSchedules.Count > 0;
                })
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

            // Generate sample chart data (replace with real logic later)
            var currentYear = DateTime.Now.Year;

            // Count bins with Completed pickups per month
            var completedPickupCounts = _context.Schedules
                .Where(s => s.Status == ScheduleStatus.Completed &&
                            s.ScheduledDate.Year == currentYear)
                .GroupBy(s => s.ScheduledDate.Month)
                .ToDictionary(g => g.Key, g => g.Select(s => s.BinId).Distinct().Count());

            // Count unique trucks used in pickups per month
            var truckUsageCounts = _context.Schedules
                .Where(s => s.ScheduledDate.Year == currentYear)
                .GroupBy(s => s.ScheduledDate.Month)
                .ToDictionary(g => g.Key, g => g.Select(s => s.TruckId).Distinct().Count());

            List<string> labels = new List<string>();
            List<int> completedPickupData = new List<int>();
            List<int> truckUsageData = new List<int>();

            for (int month = 1; month <= 12; month++)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
                labels.Add(monthName);

                completedPickupData.Add(completedPickupCounts.GetValueOrDefault(month, 0));
                truckUsageData.Add(truckUsageCounts.GetValueOrDefault(month, 0));
            }

            viewModel.MonthLabels = labels.ToArray();
            viewModel.MonthlyCompletedPickups = completedPickupData.ToArray();
            viewModel.MonthlyTrucksUsedForPickups = truckUsageData.ToArray();

            var sevenDaysAgo = today.AddDays(-6); // Last 7 days including today

            // Get all completed pickups in the last 7 days
            var dailyPickups = _context.Schedules
                .Where(s => s.Status == ScheduleStatus.Completed &&
                            s.ScheduledDate >= sevenDaysAgo &&
                            s.ScheduledDate <= today)
                .GroupBy(s => s.ScheduledDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());


            List<int> counts = new List<int>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                labels.Add(date.ToString("ddd")); // e.g., Mon, Tue...
                counts.Add(dailyPickups.GetValueOrDefault(date.Date, 0));
            }

            viewModel.DailyLabels = labels.ToArray();
            viewModel.DailyPickupCounts = counts.ToArray();
            viewModel.PickupsLast7Days = counts.Sum();

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
        public IActionResult PickupReportPreview(DateTime? startDate, DateTime? endDate, string preview = "false")
        {
            var schedules = _context.Schedules
                .Include(s => s.Truck)
                .Include(s => s.Bin)
                .AsQueryable();

            if (startDate.HasValue)
                schedules = schedules.Where(s => s.ScheduledDate >= startDate.Value);

            if (endDate.HasValue)
                schedules = schedules.Where(s => s.ScheduledDate <= endDate.Value);

            //Include both Completed and Missed
            schedules = schedules.Where(s =>
                s.Status == ScheduleStatus.Completed ||
                s.Status == ScheduleStatus.Missed);

            ViewBag.IsPreview = string.Equals(preview, "true", StringComparison.OrdinalIgnoreCase);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View("PickupReport", schedules.ToList());
        }

        public IActionResult ExportPickupReport(DateTime? startDate, DateTime? endDate)
        {
            var schedules = _context.Schedules
                .Include(s => s.Truck)
                .Include(s => s.Bin)
                .AsQueryable();

            if (startDate.HasValue)
                schedules = schedules.Where(s => s.ScheduledDate >= startDate.Value);

            if (endDate.HasValue)
                schedules = schedules.Where(s => s.ScheduledDate <= endDate.Value);

            //Include both Completed and Missed
            schedules = schedules.Where(s =>
                s.Status == ScheduleStatus.Completed ||
                s.Status == ScheduleStatus.Missed);

            return new ViewAsPdf("PickupReport", schedules.ToList())
            {
                FileName = "PickupReport.pdf"
            };
        }

        ///END PICKUP


    }
}
