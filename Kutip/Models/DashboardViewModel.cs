using System.Collections.Generic;

namespace Kutip.Models
{
    public class DashboardViewModel
    {
        public List<Bin> Bins { get; set; }
        public List<Truck> Trucks { get; set; }

        // Optional: quick summaries
        public int TotalBins => Bins?.Count ?? 0;
        public int ActiveBins => Bins?.Count(b => b.Status == BinStatus.Active) ?? 0;

        public int TotalTrucks => Trucks?.Count ?? 0;
        public int ActiveTrucks => Trucks?.Count(t => t.Status == TruckStatus.Active) ?? 0;
        public int TrucksUnderMaintenance => Trucks?.Count(t => t.Status == TruckStatus.Maintenance) ?? 0;
    }
}
