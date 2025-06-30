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
using Microsoft.AspNetCore.Identity;

namespace Kutip.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

            // ✅ Get today's date and schedule day (Malaysia Time)
            var malaysiaTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Singapore")
            );

            var todayDate = malaysiaTime.Date; // For UpdatedAt.Date
            //var todayScheduleEnum = (ScheduleDay)malaysiaTime.DayOfWeek; // For ScheduledDay enum
            var systemDayOfWeek = malaysiaTime.DayOfWeek;
            ScheduleDay todayScheduleEnum = systemDayOfWeek switch
            {
                DayOfWeek.Sunday => ScheduleDay.Sunday,
                DayOfWeek.Monday => ScheduleDay.Monday,
                DayOfWeek.Tuesday => ScheduleDay.Tuesday,
                DayOfWeek.Wednesday => ScheduleDay.Wednesday,
                DayOfWeek.Thursday => ScheduleDay.Thursday,
                DayOfWeek.Friday => ScheduleDay.Friday,
                DayOfWeek.Saturday => ScheduleDay.Saturday,
                _ => throw new Exception("Unknown day")
            };


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
            viewModel.TrucksAssignedToday = _context.Schedules
            .Where(s => s.ScheduledDay == todayScheduleEnum)
            .Select(s => s.TruckId)
            .Distinct()
            .Count();


            // Get today's date
            viewModel.BinsScheduledToday = bins
    .Select(bin => new
    {
        Bin = bin,
        TodaysSchedule = bin.Schedules
            .Where(s => s.UpdatedAt.Date == todayDate &&
                        (s.Status == ScheduleStatus.Completed || s.Status == ScheduleStatus.Missed))
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefault()
    })
    .Where(x => x.TodaysSchedule != null)
    .OrderByDescending(x => x.TodaysSchedule.UpdatedAt) // ✅ SORT by latest pickup time
    .Select(x => x.Bin)
    .ToList();

            // Missed pickups today
            viewModel.MissedPickupsToday = _context.Schedules
                .Count(s => s.ScheduledDay == todayScheduleEnum && s.UpdatedAt.Date == todayDate && s.Status == ScheduleStatus.Missed);

            // Completion rate
            int completedToday = _context.Schedules.Count(s => s.ScheduledDay == todayScheduleEnum && s.UpdatedAt.Date == todayDate && s.Status == ScheduleStatus.Completed);
            int totalHandledToday = completedToday + viewModel.MissedPickupsToday;
            viewModel.CompletionRate = totalHandledToday > 0 ? (completedToday * 100.0 / totalHandledToday) : 0;

            // Idle trucks
            var workingTruckIds = _context.Schedules
                .Where(s => s.ScheduledDay == todayScheduleEnum)
                .Select(s => s.TruckId)
                .Distinct()
                .ToList();

            viewModel.IdleTrucksToday = _context.Trucks
                .Count(t => !workingTruckIds.Contains(t.TruckId) && t.Status != TruckStatus.Maintenance);


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


        [Authorize]
        public async Task<IActionResult> Map()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userRole = User.IsInRole("Admin") ? "Admin" : "TruckDriver";
            ViewBag.UserRole = userRole;

            if (userRole == "Admin")
            {
                // Admin sees all bins with schedule info
                var binsWithSchedules = await _context.Bin
                    .Include(b => b.Schedules)
                        .ThenInclude(s => s.Truck)
                    .ToListAsync();

                return View(binsWithSchedules);
            }
            else
            {
                // Truck Driver: get only their assigned bins
                var driverName = $"{user.FirstName} {user.LastName}";

                var truck = await _context.Trucks
                    .Include(t => t.Schedules)
                        .ThenInclude(s => s.Bin)
                    .FirstOrDefaultAsync(t => t.DriverName == driverName);

                if (truck == null || !truck.Schedules.Any())
                {
                    return View(new List<Bin>());
                }

                var assignedBins = truck.Schedules
                    .Select(s => s.Bin)
                    .Distinct()
                    .ToList();

                return View(assignedBins);
            }
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
                FileName = "TruckReport.pdf",
                CustomSwitches = "--enable-local-file-access" // ✅ Enables local image rendering
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
                FileName = "BinReport.pdf",
                CustomSwitches = "--enable-local-file-access" // ✅ Enables local image rendering
            };
        }

        public IActionResult PreviewBinReport()
        {
            return RedirectToAction("BinReportPreviewWrapper", new { preview = true });
        }

        ///END BIN

        ///Pickup report
        public async Task<IActionResult> PickupReportPreview(
        DateTime? startDate,
        DateTime? endDate,
        string streetFilter = null,
        string preview = "false")
        {
            var query = _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .AsQueryable();

            // Filter for Completed and Missed only
            query = query.Where(s => s.Status == ScheduleStatus.Completed || s.Status == ScheduleStatus.Missed);

            // Optional: Also filter by date range
            if (startDate.HasValue)
                query = query.Where(s => s.UpdatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.UpdatedAt <= endDate.Value);

            // Optional: Filter by street
            if (!string.IsNullOrEmpty(streetFilter))
                query = query.Where(s => EF.Functions.Like(s.Bin.Street, $"%{streetFilter}%"));

            var results = await query.ToListAsync();

            ViewBag.IsPreview = string.Equals(preview, "true", StringComparison.OrdinalIgnoreCase);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStreet = streetFilter;

            return View("PickupReport", results);
        }

        public IActionResult ExportPickupReport(DateTime? startDate, DateTime? endDate, string streetFilter = null)
        {
            var query = _context.Schedules
                .Include(s => s.Bin)
                .Include(s => s.Truck)
                .AsQueryable();

            // ✅ Only Completed and Missed Schedules
            query = query.Where(s => s.Status == ScheduleStatus.Completed || s.Status == ScheduleStatus.Missed);

            if (startDate.HasValue)
                query = query.Where(s => s.UpdatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.UpdatedAt <= endDate.Value);

            if (!string.IsNullOrEmpty(streetFilter))
                query = query.Where(s => EF.Functions.Like(s.Bin.Street, $"%{streetFilter}%"));

            var results = query.ToList();

            return new ViewAsPdf("PickupReport", results)
            {
                FileName = "PickupReport.pdf",
                CustomSwitches = "--enable-local-file-access" // ✅ Enables local image rendering
            };
        }



    }
}
