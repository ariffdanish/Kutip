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
        [Display(Name = "Scheduled Date & Time")]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [Display(Name = "Schedule Status")]
        [EnumDataType(typeof(ScheduleStatus))]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        [Display(Name = "Created At")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [Display(Name = "Updated At")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

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
}
