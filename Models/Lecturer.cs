using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Lecturer
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string IdNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Currency)]
        public decimal DefaultHourlyRate { get; set; }

        [Required, MaxLength(50)]
        public string BankName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BankAccountNumber { get; set; } = string.Empty;
    }
}
