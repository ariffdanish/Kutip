﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Kutip.Models
{
    public class ApplicationUser : IdentityUser 
    {
        
        [StringLength(100, ErrorMessage = "First name cannot be longer than 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [StringLength(100, ErrorMessage = "Last name cannot be longer than 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
        
        [StringLength(50, ErrorMessage = "Staff ID cannot be longer than 50 characters.")]
        [Display(Name = "Staff ID")]
        public string StaffId { get; set; }
        
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }
    }
}
