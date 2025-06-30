using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kutip.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        [Display(Name = "Bin")]
        public int BinId { get; set; }

        [Required]
        [Display(Name = "Truck")]
        public int TruckId { get; set; }

        [Required]
        [Display(Name = "Day")]
        [EnumDataType(typeof(ScheduleDay))]
        public ScheduleDay ScheduledDay { get; set; }

        [Display(Name = "Schedule Date")]
        public DateTime ScheduledDate { get; set; }

       
        [Required]
        [Display(Name = "Status")]
        [EnumDataType(typeof(ScheduleStatus))]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        [Display(Name = "Created")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [Display(Name = "Updated")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
        [NotMapped]
        public string Location { get; set; }
        [NotMapped]
        public double Latitude { get; set; }
        [NotMapped]
        public double Longitude { get; set; }
        [NotMapped]
        public string Street { get; set; }

        // Navigation Properties
        public virtual Bin Bin { get; set; }
        public virtual Truck Truck { get; set; }
    }

    public enum ScheduleStatus
    {
        Scheduled,
        Completed,
        Missed,
        Reassigned
    }

    public enum ScheduleDay
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }


}
