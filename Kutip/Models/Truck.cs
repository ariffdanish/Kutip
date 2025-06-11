using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Configuration;

namespace Kutip.Models
{
    
    public class Truck
    {
        [Key]
        public int TruckId { get; set; }

        [Display(Name = "Truck No")]
        [Required]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Truck number must be alphanumeric.")]
        [StringLength(13)]
        public string TruckNo { get; set; }

        [Display(Name = "Status")]
        public TruckStatus Status { get; set; } = TruckStatus.Active;

        [Display(Name = "Driver")]
        [Required]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Driver name must contain only letters and spaces.")]
        [StringLength(100)]
        public string DriverName { get; set; }

        [Display(Name = "Phone")]
        [Required]
        [StringLength(15)]
        [RegularExpression(@"^\+?[0-9]*$", ErrorMessage = "Phone number must be numeric.")]
        public string Phone { get; set; }

        [Display(Name = "Company")]
        [Required]
        [StringLength(100)]
        public string Company { get; set; }


        [Display(Name ="Created")]
        public DateTimeOffset CreatedAt { get; set; } = DateTime.Now;
        [Display(Name = "Updated")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual List<Bin> Bins { get; set; }
        // Models/Truck.cs

        public virtual double CurrentLatitude { get; set; }
        public virtual double CurrentLongitude { get; set; }

    }

    public enum TruckStatus
    {
        Active,
        Inactive, // Truck malfunctioning
        Maintenance
    }
}
