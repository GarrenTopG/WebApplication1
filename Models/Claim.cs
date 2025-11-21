using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication1.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [BindNever]
        public string? UserId { get; set; }

        [Required, StringLength(100)]
        public string LecturerName { get; set; } = string.Empty;

        [Required, Range(1, 176)]
        public decimal HoursWorked { get; set; }

        [Required, Range(50, 1000)]
        public decimal HourlyRate { get; set; }

        [BindNever]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

        [Required, DataType(DataType.DateTime)]
        [FutureDateNotAllowed]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    }


    public class FutureDateNotAllowed : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime inputDate && inputDate > DateTime.Now)
                return new ValidationResult("Date cannot be in the future");

            return ValidationResult.Success;
        }
    }
}
