using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;

namespace WebApplication1.Services
{
    public class ClaimVerificationService
    {
        private readonly ApplicationDbContext _context;

        public ClaimVerificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public VerificationResult VerifyClaim(Claim claim)
        {
            return new VerificationResult
            {
                HourlyRateValid = CheckHourlyRate(claim),
                TotalHoursValid = CheckTotalHours(claim),
                DuplicateClaim = CheckDuplicates(claim)
            };
        }

        // Example: Check if the hourly rate exceeds a max allowed rate (e.g., 500)
        private bool CheckHourlyRate(Claim claim)
        {
            decimal maxRate = 500m; // This could be fetched from a lecturer contract table
            return claim.HourlyRate <= maxRate;
        }

        // Example: Ensure the total hours per lecturer per month do not exceed 176
        private bool CheckTotalHours(Claim claim)
        {
            var month = claim.SubmittedAt.Month;
            var year = claim.SubmittedAt.Year;

            var totalHoursThisMonth = _context.Claims
                .Where(c => c.UserId == claim.UserId &&
                            c.SubmittedAt.Month == month &&
                            c.SubmittedAt.Year == year &&
                            c.Id != claim.Id)
                .Sum(c => (decimal?)c.HoursWorked) ?? 0m;

            return (totalHoursThisMonth + claim.HoursWorked) <= 176m;
        }

        // Example: Prevent duplicate claims for same lecturer, same hours, same month
        private bool CheckDuplicates(Claim claim)
        {
            var duplicates = _context.Claims
                .Where(c => c.UserId == claim.UserId &&
                            c.HoursWorked == claim.HoursWorked &&
                            c.HourlyRate == claim.HourlyRate &&
                            c.SubmittedAt.Month == claim.SubmittedAt.Month &&
                            c.SubmittedAt.Year == claim.SubmittedAt.Year &&
                            c.Id != claim.Id)
                .Any();

            return duplicates;
        }
    }

    public class VerificationResult
    {
        public bool HourlyRateValid { get; set; }
        public bool TotalHoursValid { get; set; }
        public bool DuplicateClaim { get; set; }
    }
}
