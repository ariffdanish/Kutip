using Kutip.Models;

namespace Kutip.Models
{
    public class DashboardBinSchedule
    {
        public Bin Bin { get; set; }
        public Schedule LatestScheduleForToday { get; set; }
    }
}
