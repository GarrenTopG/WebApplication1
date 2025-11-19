namespace WebApplication1.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        //A unique ID generated when an error occurs

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        // Used in the error page to decide whether to display the ID.
    }
}
