using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;   // needed for [Precision]

namespace WebApplication1.Models
{

    // Enum for claim status (just a set of named constants.)
    // just storing "Approved" or "Rejected" strings.
    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Claim
    {
        [Key]  // primary key
        // ClaimId is the unique identifier for each claim (primary key).
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Lecturer name is required")]
        [StringLength(100)]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        [Range(1, 500, ErrorMessage = "Hours worked must be between 1 and 500")]
        [Precision(18, 2)]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Range(50, 1000, ErrorMessage = "Hourly rate must be between 50 and 1000")]
        [Precision(18, 2)]
        [Display(Name = "Hourly Rate")]
        public decimal HourlyRate { get; set; }

        // calculated field (not mapped directly)
        [Precision(18, 2)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount => HoursWorked * HourlyRate;

        [StringLength(500)]
        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        [Required]
        [Display(Name = "Claim Status")]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Display(Name = "Submitted At")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // navigation property (1 claim can have many docs)
        public ICollection<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    }
}

