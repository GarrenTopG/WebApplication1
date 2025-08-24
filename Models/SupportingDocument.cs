using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class SupportingDocument
    {
        [Key]  // primary key
        public int DocumentId { get; set; }

        [Required]  // foreign key
        public int ClaimId { get; set; }

        // navigation property (each doc belongs to one claim)
        public Claim Claim { get; set; } = default!;

        [Required(ErrorMessage = "File name is required")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "File path is required")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}

