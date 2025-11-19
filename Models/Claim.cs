using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication1.Models
{
    public class Claim
    {
        public int Id { get; set; }

        // Links claim to logged-in user
        [BindNever]
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Lecturer name is required")]
        [StringLength(100)]
        public string LecturerName { get; set; }

        [Required(ErrorMessage = "Hours Worked is required")]
        [Range(1, 176, ErrorMessage = "Hours cannot exceed 176 hours per month")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(50, 1000, ErrorMessage = "Rate must be between R50 and R1000")]
        public decimal HourlyRate { get; set; }

        // --- TOTAL AMOUNT ---
        [BindNever]  // <-- REQUIRED FIX
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [Required]
        public ClaimStatus Status { get; set; }

        [Required(ErrorMessage = "Submitted date is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Submitted At")]
        [FutureDateNotAllowed]
        public DateTime SubmittedAt { get; set; }

        public ICollection<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    }

    public class FutureDateNotAllowed : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("Date is required");

            DateTime inputDate = (DateTime)value;

            if (inputDate > DateTime.Now)
                return new ValidationResult("Date cannot be in the future");

            return ValidationResult.Success;
        }
    }
}

