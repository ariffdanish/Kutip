using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Kutip.Models
{
    // Define the enum OUTSIDE the class
    public enum BinStatus
    {
        Active,
        Inactive
    }

    public class Bin
    {
        [Key]
        public int BinId { get; set; }

        [Required(ErrorMessage = "Bin Number is required")]
        [Display(Name = "Bin")]
        [RegularExpression(@"^BIN\d{4}$", ErrorMessage = "Bin Number must be in the format 'BIN0001'")]
        public string BinNo { get; set; }

        [Required(ErrorMessage = "Street is required")]
        [Display(Name = "Street")]
        public string Street { get; set; }

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required(ErrorMessage = "State is required")]
        [Display(Name = "State")]
        public string State { get; set; }

        [Required(ErrorMessage = "PostCode is required")]
        [Display(Name = "PostCode")]
        public string PostCode { get; set; }

        [Required]
        [Display(Name = "Status")]
        public BinStatus Status { get; set; } = BinStatus.Active;

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
