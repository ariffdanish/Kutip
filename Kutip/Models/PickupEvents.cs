using System;
using System.ComponentModel.DataAnnotations;

namespace Kutip.Models
{
    public class PickupEvent
    {
        [Key]
        public int PickupEventId { get; set; }

        [Required]
        public int RelatedBinId { get; set; }

        [Required]
        public int RelatedTruckId { get; set; }

        [Required]
        public ScheduleStatus Status { get; set; } // Completed / Missed

        [Required]
        public DateTimeOffset EventRecordedAt { get; set; } = DateTimeOffset.Now;

        // Renamed from ScheduleId → RelatedScheduleId
        public int? RelatedScheduleId { get; set; }

        // Navigation properties
        public virtual Bin Bin { get; set; }
        public virtual Truck Truck { get; set; }

        // Renamed navigation property
        public virtual Schedule RelatedSchedule { get; set; }
    }
}