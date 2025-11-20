namespace WebApplication1.Models
{
    public enum ClaimStatus
    {
        Submitted = 1,             // Initial submission
        UnderReview = 2,           // Coordinator reviewing
        Approved = 3,              // Manager approved
        Processed = 4,             // HR processed
        Rejected = 5,              // Manager rejected
        SentBack = 6               // Coordinator returned for corrections
    }
}
