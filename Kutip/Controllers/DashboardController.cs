using Microsoft.AspNetCore.Mvc;
using Kutip.Models;
using System.Linq;
using System.Collections.Generic;
using Kutip.Data;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


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
            //var binsQuery = _context.Bin.AsQueryable();
            var binsQuery = _context.Bin.Include(b => b.Schedules).AsQueryable();
            var trucksQuery = _context.Trucks.AsQueryable();
            //var binsWithSchedules = binsQuery.ToList();



            // Bin filters
            if (!string.IsNullOrEmpty(cityFilter))
                binsQuery = binsQuery.Where(b => b.City == cityFilter);

            if (!string.IsNullOrEmpty(stateFilter))
                binsQuery = binsQuery.Where(b => b.State == stateFilter);

            if (binStatusFilter.HasValue)
                binsQuery = binsQuery.Where(b => b.Status == binStatusFilter.Value);

            // Truck filters
            if (!string.IsNullOrEmpty(truckNoFilter))
                trucksQuery = trucksQuery.Where(t => t.TruckNo.Contains(truckNoFilter));

            if (truckStatusFilter.HasValue)
                trucksQuery = trucksQuery.Where(t => t.Status == truckStatusFilter.Value);

            var viewModel = new DashboardViewModel
            {
                Bins = binsQuery.ToList(),
                Trucks = trucksQuery.ToList(),
               

            };

            // *** New code starts here: filter bins to only those with latest schedule Completed ***
            var binsCompleted = viewModel.Bins.Where(bin =>
            {
                var latestSchedule = bin.Schedules
                    .OrderByDescending(s => s.ScheduledDateTime)
                    .FirstOrDefault();
                return latestSchedule != null && latestSchedule.Status == ScheduleStatus.Completed;
            }).ToList();

            ViewBag.BinsCompleted = binsCompleted;
            // *** New code ends here ***

            // Inject latest schedule info into ViewBag (linked by BinId)
            var latestScheduleStatusByBinId = viewModel.Bins
                .ToDictionary(
                    bin => bin.BinId,
                    bin => bin.Schedules
                        .OrderByDescending(s => s.ScheduledDateTime)
                        .FirstOrDefault()
                );

            ViewBag.ScheduleLookup = latestScheduleStatusByBinId;

            // Dropdown data
            ViewBag.AllCities = _context.Bin.Select(b => b.City).Distinct().ToList();
            ViewBag.AllStates = _context.Bin.Select(b => b.State).Distinct().ToList();
            ViewBag.AllBinStatuses = Enum.GetValues(typeof(BinStatus)).Cast<BinStatus>().ToList();
            ViewBag.AllTruckStatuses = Enum.GetValues(typeof(TruckStatus)).Cast<TruckStatus>().ToList();
            ViewBag.AllTruckNos = _context.Trucks.Select(t => t.TruckNo).Distinct().ToList();

            // Selected filters
            ViewBag.SelectedCity = cityFilter;
            ViewBag.SelectedState = stateFilter;
            ViewBag.SelectedBinStatus = binStatusFilter;
            ViewBag.SelectedTruckNo = truckNoFilter;
            ViewBag.SelectedTruckStatus = truckStatusFilter;

            return View(viewModel);
        }
    }
}
