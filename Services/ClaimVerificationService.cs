using WebApplication1.Data;
using WebApplication1.Models;

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
            var result = new VerificationResult
            {
                HourlyRateValid = CheckHourlyRate(claim),
                TotalHoursValid = CheckTotalHours(claim),
                DuplicateClaim = CheckDuplicates(claim)
            };

            return result;
        }

        private bool CheckHourlyRate(Claim claim)
        {
            // Implement logic to compare against lecturer's contracted rate
            return true;
        }

        private bool CheckTotalHours(Claim claim)
        {
            // Implement logic to check monthly total hours
            return true;
        }

        private bool CheckDuplicates(Claim claim)
        {
            // Implement logic to check for duplicate claims
            return false;
        }
    }

    public class VerificationResult
    {
        public bool HourlyRateValid { get; set; }
        public bool TotalHoursValid { get; set; }
        public bool DuplicateClaim { get; set; }
    }
}

