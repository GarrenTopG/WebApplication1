using WebApplication1.Services;

namespace WebApplication1.Models
{
    public class PendingClaimViewModel
    {
        public Claim Claim { get; set; }                     // The claim itself
        public VerificationResult? Verification { get; set; } // Only for Coordinators
    }
}
