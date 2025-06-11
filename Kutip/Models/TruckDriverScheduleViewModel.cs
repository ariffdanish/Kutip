namespace Kutip.Models
{
    public class TruckDriverScheduleViewModel
    {
        public DateTime ScheduledDateTime { get; set; }
        public string BinId { get; set; }
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TruckId { get; set; }
    }

}
