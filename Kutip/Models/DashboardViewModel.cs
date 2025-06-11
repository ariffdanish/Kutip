using System.Collections.Generic;

namespace Kutip.Models
{
    public class DashboardViewModel
    {
        public List<Bin> Bins { get; set; }
        public List<Truck> Trucks { get; set; }

        // Stats
        public int TotalBins => Bins?.Count ?? 0;
        public int ActiveBins => Bins?.Count(b => b.Status == BinStatus.Active) ?? 0;
        public int TotalTrucks => Trucks?.Count ?? 0;
        public int TrucksUnderMaintenance => Trucks?.Count(t => t.Status == TruckStatus.Maintenance) ?? 0;
        public int TrucksAssignedToday { get; set; }

        // Chart Data (NEW)
        public int[] MonthlyCompletedPickups { get; set; }
        public int[] MonthlyTrucksUsedForPickups { get; set; }
        public string[] MonthLabels { get; set; }
        public int[] DailyPickupCounts { get; set; }
        public string[] DailyLabels { get; set; }
        public int PickupsLast7Days { get; set; }



        // Dropdowns
        public List<string> AllCities { get; set; }
        public List<string> AllStates { get; set; }
        public List<BinStatus> AllBinStatuses { get; set; }
        public List<TruckStatus> AllTruckStatuses { get; set; }
        public List<string> AllTruckNos { get; set; }

        // Selected Filters
        public string SelectedCity { get; set; }
        public string SelectedState { get; set; }
        public BinStatus? SelectedBinStatus { get; set; }
        public string SelectedTruckNo { get; set; }
        public TruckStatus? SelectedTruckStatus { get; set; }

        // Schedule-related
        public Dictionary<int, Schedule> ScheduleLookup { get; set; }
        public List<Bin> BinsScheduledToday { get; set; }

        // Helper Properties
        public Bin GetLatestScheduleBin(int binId)
        {
            return Bins?.FirstOrDefault(b => b.BinId == binId);
        }

        public Schedule GetLatestSchedule(int binId)
        {
            if (ScheduleLookup != null && ScheduleLookup.TryGetValue(binId, out var schedule))
            {
                return schedule;
            }
            return null;
        }
    }
}
